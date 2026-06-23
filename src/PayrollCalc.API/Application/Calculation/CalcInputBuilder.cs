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
        // Читаємо лише для мапінгу в CalcInput (граф не змінюється й не зберігається) — без трекінгу.
        var employee = await WithCalcIncludes(db.Employees.AsNoTracking())
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null)
            return null;

        var calendar = await LoadCalendarAsync(year, month, ct);
        var paramMap = await LoadParamsAsync(year, month, ct);
        var timesheet = await db.Timesheets
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Year == year && t.Month == month, ct);
        var absences = await LoadAbsencesAsync(employeeId, new DateOnly(year, month, 1), ct);

        return AssembleInput(employee, calendar, timesheet, paramMap, absences, year, month);
    }

    /// <summary>
    /// Будує вхід рушія для ВСІХ працівників місяця за кілька запитів (а не по запиту на людину, що
    /// було повільно через важкий include-граф). Один include усіх, один календар/параметри, гуртові
    /// табелі й відсутності; збірка кожного CalcInput — у пам'яті. Порядок — український алфавіт.
    /// Викликається з RunAllAsync (відомість/прогон усіх).
    /// </summary>
    public async Task<IReadOnlyList<CalcInput>> BuildAllAsync(int year, int month, CancellationToken ct = default)
    {
        var periodStart = new DateOnly(year, month, 1);
        var calendar = await LoadCalendarAsync(year, month, ct);
        var paramMap = await LoadParamsAsync(year, month, ct);

        // Активні + звільнені всередині/після місяця (звільнений лишається у відомості свого місяця).
        var employees = await WithCalcIncludes(db.Employees
                .AsNoTracking()
                .Where(e => e.Status != EmployeeStatus.Dismissed
                    || (e.DismissalDate != null && e.DismissalDate >= periodStart))
                .OrderBy(e => EF.Functions.Collate(e.FullName, "uk-UA-x-icu")))
            .ToListAsync(ct);

        // Табелі й відсутності — гуртом за місяць, далі розкладаємо по працівнику в пам'яті.
        var timesheets = (await db.Timesheets
                .Where(t => t.Year == year && t.Month == month)
                .ToListAsync(ct))
            .GroupBy(t => t.EmployeeId)
            .ToDictionary(g => g.Key, g => g.First());
        var absencesByEmp = await LoadAllAbsencesAsync(periodStart, ct);

        return employees
            .Select(e => AssembleInput(
                e, calendar, timesheets.GetValueOrDefault(e.Id), paramMap,
                absencesByEmp.GetValueOrDefault(e.Id) ?? new AbsenceAmounts(), year, month))
            .ToList();
    }

    /// <summary>
    /// Include-граф ставки з усіма блоками (Position/розряд/звання/навантаження/адмін/непед/ГПД/ПКР) —
    /// спільний для одиночного й гуртового завантаження.
    /// </summary>
    private static IQueryable<Employee> WithCalcIncludes(IQueryable<Employee> query) =>
        query
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .Include(e => e.Positions).ThenInclude(p => p.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.TitleType)
            .Include(e => e.Positions).ThenInclude(p => p.Workload).ThenInclude(w => w!.NotebookRate)
            .Include(e => e.Positions).ThenInclude(p => p.Admin)
            .Include(e => e.Positions).ThenInclude(p => p.NonPedagogical)
            .Include(e => e.Positions).ThenInclude(p => p.Gpd).ThenInclude(g => g!.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.Pkr).ThenInclude(k => k!.TariffGrade);

    /// <summary>
    /// Робочий календар місяця з валідацією: немає → кидає; норма ≤ 0 → кидає (далі вона дільник пропорції).
    /// </summary>
    private async Task<WorkCalendar> LoadCalendarAsync(int year, int month, CancellationToken ct)
    {
        var calendar = await db.WorkCalendars.FirstOrDefaultAsync(wc => wc.Year == year && wc.Month == month, ct)
            ?? throw new InvalidOperationException($"Немає робочого календаря за {month:00}.{year}.");
        if (calendar.WorkDays <= 0)
            throw new InvalidOperationException($"Невалідна норма робочих днів ({calendar.WorkDays}) за {month:00}.{year}.");
        return calendar;
    }

    /// <summary>
    /// Збирає CalcInput у пам'яті з уже завантажених сутностей (без звернень до БД): активні ставки,
    /// заміни/«заслужений» на першу відповідну ставку, робочі дні мінус дні відсутностей.
    /// </summary>
    private static CalcInput AssembleInput(Employee employee, WorkCalendar calendar, Timesheet? timesheet,
        Dictionary<string, decimal> paramMap, AbsenceAmounts absences, int year, int month)
    {
        // Ставки, чинні бодай день у цьому місяці: активні + звільнені всередині/після нього.
        var periodStart = new DateOnly(year, month, 1);
        var activePositions = employee.Positions
            .Where(p => p.DismissalDate is null || p.DismissalDate >= periodStart)
            .Select(p => MapPosition(p, employee, timesheet))
            .ToList();

        // Заміни уроків — у табелі на працівника, платяться від учительської ставки. Кладемо години на
        // першу педагогічну, щоб багатоставковий (директор-вчитель) не отримав оплату двічі.
        var replacementHours = timesheet?.ReplacementHours ?? 0m;
        if (replacementHours != 0)
        {
            var teacherIdx = activePositions.FindIndex(p => p.WorkerClass == WorkerClass.Pedagogical);
            if (teacherIdx >= 0)
                activePositions[teacherIdx] = activePositions[teacherIdx] with { ReplacementHours = replacementHours };
        }

        // «Заслужений» — на працівника (фіксована сума, Class 1/2). Кладемо на першу пед/адмін-пед
        // ставку, щоб у багатоставкового не задвоїлась; лягає в колонку звання.
        if (employee.IsHonored && employee.HonoredAmount is { } honored && honored != 0)
        {
            var honoredIdx = activePositions.FindIndex(p =>
                p.WorkerClass is WorkerClass.Pedagogical or WorkerClass.AdminPedagogical);
            if (honoredIdx >= 0)
                activePositions[honoredIdx] = activePositions[honoredIdx] with { HonoredAmount = honored };
        }

        // Дні відсутностей знімаємо з відпрацьованих: оклад і пропорційні надбавки падають за час
        // відсутності, а сама подія доплачується середньоденною окремим компонентом.
        var scheduledDays = timesheet?.WorkedDays ?? calendar.WorkDays;
        var workedDays = Math.Max(0m, scheduledDays - absences.WorkingDaysAbsent);

        return new CalcInput
        {
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            TaxId = employee.TaxId,
            Year = year,
            Month = month,
            NormDays = calendar.WorkDays,
            WorkedDays = workedDays,
            SocialBenefitPct = employee.SocialBenefitPct,
            IsUnionMember = employee.IsUnionMember,
            Positions = activePositions,
            Manual = MapManual(timesheet),
            Absences = absences,
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
    /// Тягне події відсутності працівника, що починаються в цьому місяці: складає вже пораховані
    /// суми (рахував сервіс при вводі — тут лише читаємо) і сумарні робочі дні (їх BuildAsync зніме
    /// з відпрацьованих). Компенсація відпустки дає гроші, але днів НЕ знімає — у ці дні працівник
    /// працює. Викликається з BuildAsync, результат → CalcInput.Absences.
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
        return BuildAbsence(sick, vacations, training);
    }

    /// <summary>
    /// Гуртом тягне всі відсутності місяця (3 запити замість 3 на людину) і групує за працівником —
    /// для прогону всіх. Працівника без відсутностей у словнику немає (далі — порожні суми).
    /// </summary>
    private async Task<Dictionary<int, AbsenceAmounts>> LoadAllAbsencesAsync(DateOnly periodStart, CancellationToken ct)
    {
        var periodEnd = periodStart.AddMonths(1);
        var sick = await db.SickLeaves.Where(s => s.StartDate >= periodStart && s.StartDate < periodEnd).ToListAsync(ct);
        var vacations = await db.Vacations.Where(v => v.StartDate >= periodStart && v.StartDate < periodEnd).ToListAsync(ct);
        var training = await db.TrainingLeaves.Where(t => t.StartDate >= periodStart && t.StartDate < periodEnd).ToListAsync(ct);

        var ids = sick.Select(s => s.EmployeeId)
            .Concat(vacations.Select(v => v.EmployeeId))
            .Concat(training.Select(t => t.EmployeeId))
            .Distinct();
        return ids.ToDictionary(id => id, id => BuildAbsence(
            sick.Where(s => s.EmployeeId == id).ToList(),
            vacations.Where(v => v.EmployeeId == id).ToList(),
            training.Where(t => t.EmployeeId == id).ToList()));
    }

    /// <summary>
    /// Складає суми відсутностей з уже завантажених подій. Компенсація відпустки дає гроші, але днів
    /// не знімає (працівник у ці дні працює) — у сумі днів її нема.
    /// </summary>
    private static AbsenceAmounts BuildAbsence(
        IReadOnlyList<SickLeave> sick, IReadOnlyList<Vacation> vacations, IReadOnlyList<TrainingLeave> training)
    {
        return new AbsenceAmounts
        {
            SickEmployer = sick.Sum(s => s.AmountEmployer),
            SickFss = sick.Sum(s => s.AmountFss),
            Vacation = vacations.Where(v => v.VacationType != VacationType.Compensation).Sum(v => v.TotalAmount ?? 0m),
            VacationCompensation = vacations.Where(v => v.VacationType == VacationType.Compensation).Sum(v => v.TotalAmount ?? 0m),
            Courses = training.Sum(t => t.TotalAmount),
            WorkingDaysAbsent =
                sick.Sum(s => s.WorkingDaysAbsent)
                + vacations.Where(v => v.VacationType != VacationType.Compensation).Sum(v => v.WorkingDaysAbsent)
                + training.Sum(t => t.WorkingDaysAbsent),
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
        // Вислуга фіксована за стажем: педагогам — пед.стаж, спеціалістам — загальний.
        // МОП (Class 4) вислуги не мають — прибиральниці/сторожі/двірники без надбавки.
        // Та сама ставка йде і в основну вислугу, і в блок ГПД/ПКР.
        var tenurePct = workerClass == WorkerClass.MOP
            ? 0m
            : TenureRate.ForYears(TenureYears(workerClass, employee));
        return new PositionCalcInput
        {
            WorkerClass = workerClass,
            PositionName = ep.Position.Name,
            TariffGrade = ep.TariffGrade!.Grade,
            TitleName = ep.TitleType?.Name,
            // Директорозалежна посада: тариф = оклад директора (за конвенцією заповнення TariffGrade), множник у DirectorPct.
            Oklad = ep.TariffGrade!.MonthlyRate,
            RateCount = ep.RateCount,
            IsPrimary = ep.IsPrimary,
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
            IsHourly = isHourly,
            // Нічні — нічним працівникам: погодинний сторож (isHourly) АБО будь-хто з прапором
            // нічних змін (прибиральниця на окладі). NightShiftCalculator рахує оклад/176 × години.
            NightHours = isHourly || (nonPed?.HasNightShifts ?? false) ? timesheet?.NightHours ?? 0m : 0m,
            // Відпрацьовані години — лише погодинній ставці (вчителі) для погодинної оплати.
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
            Recalculation = t.Recalculation,
            Holiday = t.HolidayAmount,
            AnnualBonus = t.AnnualBonus,
            EnforcementOrders = t.EnforcementOrders,
            PhysEducation = t.PhysEducation,
            Downtime = t.Downtime,
            Indexation = t.Indexation,
            UnfavorableManual = t.UnfavorableManual,
        };
}
