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
    /// Будує вхід рушія для працівника на (year, month). null — працівника немає.
    /// Нема табеля → дні = норма місяця, ручні суми = 0. Нема календаря → кидає (нема з чого брати норму).
    /// </summary>
    public async Task<CalcInput?> BuildAsync(int employeeId, int year, int month, CancellationToken ct = default)
    {
        var employee = await db.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .Include(e => e.Positions).ThenInclude(p => p.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.TitleType)
            .Include(e => e.Positions).ThenInclude(p => p.Workload)
            .Include(e => e.Positions).ThenInclude(p => p.Admin)
            .Include(e => e.Positions).ThenInclude(p => p.NonPedagogical)
            .Include(e => e.Positions).ThenInclude(p => p.Gpd).ThenInclude(g => g!.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.Pkr).ThenInclude(k => k!.TariffGrade)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null)
            return null;

        var calendar = await db.WorkCalendars.FirstOrDefaultAsync(wc => wc.Year == year && wc.Month == month, ct)
            ?? throw new InvalidOperationException($"Немає робочого календаря за {month:00}.{year}.");

        var timesheet = await db.Timesheets
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Year == year && t.Month == month, ct);

        var paramMap = await LoadParamsAsync(year, month, ct);

        // Тільки чинні ставки (не звільнені на дату періоду).
        var activePositions = employee.Positions
            .Where(p => p.DismissalDate is null)
            .Select(p => MapPosition(p, employee, timesheet))
            .ToList();

        return new CalcInput
        {
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            TaxId = employee.TaxId,
            Year = year,
            Month = month,
            NormDays = calendar.WorkDays,
            WorkedDays = timesheet?.WorkedDays ?? calendar.WorkDays,
            Positions = activePositions,
            Manual = MapManual(timesheet),
            Params = PayrollParamsFactory.From(paramMap),
        };
    }

    private async Task<Dictionary<string, decimal>> LoadParamsAsync(int year, int month, CancellationToken ct)
    {
        // Знімок параметрів, чинних на початок періоду: по кожному ключу — останнє значення ≤ дати.
        var periodStart = new DateOnly(year, month, 1);
        var rows = await db.SystemParams
            .Where(sp => sp.EffectiveDate <= periodStart)
            .ToListAsync(ct);
        return rows
            .GroupBy(sp => sp.Key)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(sp => sp.EffectiveDate).First().Value);
    }

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
            ClassManagementGroup = admin is { HasClassMgmt: true } ? admin.ClassGradeGroup : null,
            Cabinet = admin is { HasCabinet: true } ? admin.CabinetType : null,
            HasComputerMaintenance = admin?.HasComputers ?? false,
            HasWebsite = admin?.HasWebsite ?? false,
            IsMentor = nonPed?.HasMentor ?? false,
            HasMilitaryRecord = ep.HasMilitaryRecord,
            HasDisinfectants = nonPed?.HasDisinfectants ?? false,
            IsLibraryHead = nonPed?.HasLibraryMgmt ?? false,
            HasTextbooks = nonPed?.HasTextbooks ?? false,
            // Нічні/години — лише на погодинній ставці (сторож); табель на працівника, віддаємо погодинній посаді.
            IsHourly = isHourly,
            NightHours = isHourly ? timesheet?.NightHours ?? 0m : 0m,
            WorkedHours = isHourly ? timesheet?.WorkedHours ?? 0m : 0m,
            ExtendedActivity = MapExtendedActivity(ep, tenurePct),
        };
    }

    /// <summary>
    /// Блок позаурочної роботи (ГПД/ПКР) ставки. Пріоритет ПКР, далі ГПД; null — немає жодного.
    /// База: ПКР — тариф/18×год; ГПД — оклад×ставка (Divisor=1, GpdHours = к-сть ставок ГПД). Проре по днях.
    /// </summary>
    private static ExtendedActivityInput? MapExtendedActivity(EmployeePosition ep, decimal tenurePct)
    {
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
                Hours = gpd.GpdHours,            // к-сть ставок ГПД (0.5 / 1.0), не години
                TenurePct = tenurePct,
                ProrateByDays = true,
            };

        return null;
    }

    /// <summary>
    /// Стаж для вислуги: педагогічні класи рахують пед.стаж, решта — загальний.
    /// </summary>
    private static int TenureYears(WorkerClass wc, Employee e)
        => wc is WorkerClass.Pedagogical or WorkerClass.AdminPedagogical
            ? e.PedExperienceYears
            : e.GeneralExperienceYears;

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
            EnforcementOrders = t.EnforcementOrders,
        };
}
