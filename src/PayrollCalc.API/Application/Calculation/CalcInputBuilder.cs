using Microsoft.EntityFrameworkCore;
using PayrollCalc.Calculation;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Calculation;

/// <summary>
/// Збирає CalcInput на одного працівника за місяць із БД: ставки + блоки + табель + довідники.
/// Рушій (Calculation) лишається чистим — уся «грязь» мапінгу БД→DTO живе тут.
/// </summary>
public class CalcInputBuilder(AppDbContext db)
{
    /// <summary>
    /// Будує вхід рушія (CalcInput) на працівника за (year, month) — єдина публічна точка білдера.
    /// Нема табеля → дні = норма місяця, ручні = 0. Нема календаря → кидає. null — працівника немає.
    /// Викликається з PayrollCalculationService.RunAsync (на кожного працівника); результат → рушій.
    /// </summary>
    public async Task<CalcInput?> BuildAsync(int employeeId, int year, int month, CancellationToken ct = default)
    {
        var employee = await db.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .Include(e => e.Positions).ThenInclude(p => p.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.TitleType)
            .Include(e => e.Positions).ThenInclude(p => p.Workload).ThenInclude(w => w!.NotebookRate)
            .Include(e => e.Positions).ThenInclude(p => p.Admin)
            .Include(e => e.Positions).ThenInclude(p => p.NonPedagogical)
            .Include(e => e.Positions).ThenInclude(p => p.Gpd).ThenInclude(g => g!.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.Pkr).ThenInclude(k => k!.TariffGrade)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null)
            return null;

        var calendar = await db.WorkCalendars.FirstOrDefaultAsync(wc => wc.Year == year && wc.Month == month, ct)
            ?? throw new InvalidOperationException($"Немає робочого календаря за {month:00}.{year}.");
        // Норма 0 далі стала б дільником пропорції /норма×відпрацьовано — ділення на нуль.
        if (calendar.WorkDays <= 0)
            throw new InvalidOperationException($"Невалідна норма робочих днів ({calendar.WorkDays}) за {month:00}.{year}.");

        var timesheet = await db.Timesheets
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Year == year && t.Month == month, ct);

        var paramMap = await LoadParamsAsync(year, month, ct);

        // Ставки, чинні бодай день у цьому місяці: активні + звільнені всередині/після нього.
        // Звільнений серед місяця лишається у відомості свого місяця (дні бере з табеля).
        var periodStart = new DateOnly(year, month, 1);
        var activePositions = employee.Positions
            .Where(p => p.DismissalDate is null || p.DismissalDate >= periodStart)
            .Select(p => MapPosition(p, employee, timesheet))
            .ToList();

        // Заміни уроків — у табелі на працівника, а платяться від учительської ставки
        // (формула від тарифу вчителя). Кладемо години на першу педагогічну, щоб
        // багатоставковий (директор-вчитель) не отримав оплату двічі.
        var replacementHours = timesheet?.ReplacementHours ?? 0m;
        if (replacementHours != 0)
        {
            var teacherIdx = activePositions.FindIndex(p => p.WorkerClass == WorkerClass.Pedagogical);
            if (teacherIdx >= 0)
                activePositions[teacherIdx] = activePositions[teacherIdx] with { ReplacementHours = replacementHours };
        }

        return new CalcInput
        {
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            TaxId = employee.TaxId,
            Year = year,
            Month = month,
            NormDays = calendar.WorkDays,
            WorkedDays = timesheet?.WorkedDays ?? calendar.WorkDays,
            SocialBenefitPct = employee.SocialBenefitPct,
            Positions = activePositions,
            Manual = MapManual(timesheet),
            Absences = await LoadAbsencesAsync(employeeId, periodStart, ct),
            Params = PayrollParamsFactory.From(paramMap),
        };
    }

    /// <summary>
    /// Версійний знімок SystemParams: по кожному ключу — останнє значення з EffectiveDate ≤ початок місяця.
    /// Так розрахунок за минулий місяць бере ставки/МЗП, чинні ТОДІ, а не сьогоднішні.
    /// Викликається з BuildAsync; результат → PayrollParamsFactory.From.
    /// </summary>
    private async Task<Dictionary<string, decimal>> LoadParamsAsync(int year, int month, CancellationToken ct)
    {
        var periodStart = new DateOnly(year, month, 1);
        var rows = await db.SystemParams
            .Where(sp => sp.EffectiveDate <= periodStart)
            .ToListAsync(ct);
        return rows
            .GroupBy(sp => sp.Key)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(sp => sp.EffectiveDate).First().Value);
    }

    /// <summary>
    /// Тягне події відсутності працівника, що починаються в цьому місяці, і складає їх уже
    /// пораховані суми (рахував їх сервіс при вводі — тут лише читаємо). Дні не чіпаємо: вони
    /// лишаються в табелі. Компенсація відпустки йде окремо — у відомості це інша колонка.
    /// Викликається з BuildAsync, результат → CalcInput.Absences.
    /// </summary>
    private async Task<AbsenceAmounts> LoadAbsencesAsync(int employeeId, DateOnly periodStart, CancellationToken ct)
    {
        var periodEnd = periodStart.AddMonths(1);
        var sick = await db.SickLeaves
            .Where(s => s.EmployeeId == employeeId && s.StartDate >= periodStart && s.StartDate < periodEnd)
            .ToListAsync(ct);
        var vacations = await db.Vacations
            .Where(v => v.EmployeeId == employeeId && v.StartDate >= periodStart && v.StartDate < periodEnd)
            .ToListAsync(ct);
        var training = await db.TrainingLeaves
            .Where(t => t.EmployeeId == employeeId && t.StartDate >= periodStart && t.StartDate < periodEnd)
            .ToListAsync(ct);

        return new AbsenceAmounts
        {
            SickEmployer = sick.Sum(s => s.AmountEmployer),
            SickFss = sick.Sum(s => s.AmountFss),
            Vacation = vacations.Where(v => v.VacationType != VacationType.Compensation).Sum(v => v.TotalAmount ?? 0m),
            VacationCompensation = vacations.Where(v => v.VacationType == VacationType.Compensation).Sum(v => v.TotalAmount ?? 0m),
            Courses = training.Sum(t => t.TotalAmount),
        };
    }

    /// <summary>
    /// Мапить одну ставку (EmployeePosition + блоки) у плоский PositionCalcInput для рушія:
    /// дерайвить TenurePct зі стажу, тягне %-и й прапори надбавок з блоків, hourly-години з табеля.
    /// Викликається з BuildAsync по кожній активній ставці працівника.
    /// </summary>
    private static PositionCalcInput MapPosition(EmployeePosition ep, Employee employee, Timesheet? timesheet)
    {
        var workerClass = ep.Position!.WorkerClass;
        var isHourly = ep.Position.IsHourly;
        var admin = ep.Admin;
        var nonPed = ep.NonPedagogical;
        var workload = ep.Workload;
        // Вислуга фіксована за стажем: педагогам — пед.стаж, спеціалістам/МОП — загальний.
        // Та сама ставка йде і в основну вислугу, і в блок ГПД/ПКР.
        var tenurePct = TenureRate.ForYears(TenureYears(workerClass, employee));

        return new PositionCalcInput
        {
            WorkerClass = workerClass,
            PositionName = ep.Position.Name,
            TariffGrade = ep.TariffGrade!.Grade,
            TitleName = ep.TitleType?.Name,
            // Директорозалежна посада: тариф = оклад директора (за конвенцією заповнення TariffGrade), множник у DirectorPct.
            Oklad = ep.TariffGrade!.MonthlyRate,
            RateCount = ep.RateCount,
            DirectorPct = ep.DirectorPct,
            TitlePct = ep.TitleType?.Pct ?? 0m,
            TenurePct = tenurePct,
            PrestigePct = ep.PrestigeBonusPct ?? 0m,
            ComplexityPct = ep.ComplexityBonusPct ?? 0m,
            PedHoursWeekly = workload is null ? 0m : workload.Hours1To4 + workload.Hours5To9 + workload.Hours10To11,
            AdditionalHours = workload?.AdditionalHours ?? 0m,
            // Зошити: години з блоку навантаження, % — з довідника за предметом (NotebookRate).
            NotebookHours = workload is null ? 0m
                : workload.NotebookHours1To4 + workload.NotebookHours5To9 + workload.NotebookHours10To11,
            NotebookPct = workload?.NotebookRate?.Pct ?? 0m,
            // Інклюзив: учителю — реальні години; адміну калькулятор трактує >0 як прапорець участі (flat 20%).
            InclusiveHours = workload is null ? 0m
                : workload.InclusiveHours1To4 + workload.InclusiveHours5To9 + workload.InclusiveHours10To11,
            HasUnfavorable2600 = ep.HasUnfavorable,
            ClassManagementGroup = admin is { HasClassMgmt: true } ? admin.ClassGradeGroup : null,
            Cabinet = admin is { HasCabinet: true } ? admin.CabinetType : null,
            HasComputerMaintenance = admin?.HasComputers ?? false,
            HasWebsite = admin?.HasWebsite ?? false,
            IsMentor = nonPed?.HasMentor ?? false,
            MaintainsMilitaryRecords = ep.MaintainsMilitaryRecords,
            HasDisinfectants = nonPed?.HasDisinfectants ?? false,
            IsLibraryHead = nonPed?.HasLibraryMgmt ?? false,
            HasTextbooks = nonPed?.HasTextbooks ?? false,
            // Спец-вислуга (бібл/мед) — ознака живе на посаді (Position.SpecialTenure), не в мапері.
            HasLibrarianTenure = ep.Position.SpecialTenure == SpecialTenureKind.Librarian,
            HasMedicTenure = ep.Position.SpecialTenure == SpecialTenureKind.Medic,
            // Нічні/години — лише на погодинній ставці (сторож); табель на працівника, віддаємо погодинній посаді.
            IsHourly = isHourly,
            NightHours = isHourly ? timesheet?.NightHours ?? 0m : 0m,
            WorkedHours = isHourly ? timesheet?.WorkedHours ?? 0m : 0m,
            ExtendedActivity = MapExtendedActivity(ep, tenurePct),
        };
    }

    /// <summary>
    /// Блок позаурочної роботи (ГПД/ПКР) ставки → ExtendedActivityInput. Пріоритет ПКР, далі ГПД; null — немає жодного.
    /// База: ПКР — тариф/18×год; ГПД — оклад×ставка (Divisor=1, GpdRate = к-сть ставок ГПД). Проре по днях.
    /// Викликається з MapPosition.
    /// </summary>
    private static ExtendedActivityInput? MapExtendedActivity(EmployeePosition ep, decimal tenurePct)
    {
        // ГПД і ПКР на одній ставці взаємовиключні (підтв. бухгалтером) — співіснувати не можуть,
        // тому порядок перевірки ролі не грає: береться той блок, що заведений.
        if (ep.Pkr is { } pkr)
            return new ExtendedActivityInput
            {
                Kind = ExtendedActivityKind.Pkr,
                Tariff = pkr.TariffGrade!.MonthlyRate,
                Divisor = 18m,
                Hours = pkr.PkrHours,
                TenurePct = tenurePct,
                ProrateByDays = true,
            };

        if (ep.Gpd is { } gpd)
            return new ExtendedActivityInput
            {
                Kind = ExtendedActivityKind.Gpd,
                Tariff = gpd.TariffGrade!.MonthlyRate,
                Divisor = 1m,
                Hours = gpd.GpdRate,            // к-сть ставок ГПД (0.5 / 1.0), не години
                TenurePct = tenurePct,
                ProrateByDays = true,
            };

        return null;
    }

    /// <summary>
    /// Стаж для вислуги: педагогічні класи (1/2) рахують пед.стаж, решта (3/4) — загальний.
    /// Викликається з MapPosition, годує TenureRate.ForYears.
    /// </summary>
    private static int TenureYears(WorkerClass wc, Employee e)
        => wc is WorkerClass.Pedagogical or WorkerClass.AdminPedagogical
            ? e.PedExperienceYears
            : e.GeneralExperienceYears;

    /// <summary>
    /// Грошові поля табеля (премія, лікарняні, відпускні, індексація…) → ManualAdjustments.
    /// Табеля нема → порожній об'єкт (усі ручні = 0).
    /// Викликається з BuildAsync.
    /// </summary>
    private static ManualAdjustments MapManual(Timesheet? t) => t is null
        ? new ManualAdjustments()
        : new ManualAdjustments
        {
            Bonus = t.Bonus,
            Advance = t.Advance,
            SickEmployer = t.SickEmployer,
            SickFss = t.SickFss,
            Recalculation = t.Recalculation,
            Vacation = t.Vacation,
            Holiday = t.HolidayAmount,
            AnnualBonus = t.AnnualBonus,
            EnforcementOrders = t.EnforcementOrders,
            PhysEducation = t.PhysEducation,
            VacationCompensation = t.VacationCompensation,
            Downtime = t.Downtime,
            Courses = t.Courses,
            Indexation = t.Indexation,
        };
}
