using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.API.Application.AvgSalary;

/// <summary>
/// Авто-заповнення бази середньоденної з підписаної історії перед проводкою події.
/// У режимі Auto бере числівник і дні з AvgBaseBuilder і кладе у base-поля події (як це зробив би
/// бухгалтер вручну). Повертає true, якщо база реально заповнена з історії; false — якщо Manual,
/// неоплачуваний тип або історії замало (тоді контролер просить ручний режим). Розрахунок — далі в AvgSalaryService.
/// </summary>
public class AvgBaseAutoFiller(AvgBaseBuilder builder)
{
    // Повне вікно: 12 міс для лікарняних/відпускних, 2 міс для курсів.
    private const int FullWindowMonths = 12;
    private const int TrainingWindowMonths = 2;
    // Календарний знаменник відпускних до вирахування виключених днів (КМУ №100).
    private const int CalendarYearDays = 365;

    /// <summary>
    /// Лікарняний: база = Σ підписаних нарахувань за 12 міс; виключені дні = минулі лікарняні +
    /// відпустки за свій рахунок. Заповнює і повертає true, лише якщо підписано всі 12 місяців.
    /// </summary>
    public async Task<bool> FillSickAsync(SickLeave e, CancellationToken ct = default)
    {
        if (e.BaseCalculationMode != CalcMode.Auto)
            return false;
        var b = await builder.BuildAmountAsync(e.EmployeeId, e.StartDate.Year, e.StartDate.Month, AvgBaseKind.Sick, ct);
        if (b.SignedMonths < FullWindowMonths)
            return false;
        e.BaseAmount = b.Amount;
        e.BaseExcludedDays = await builder.BuildExcludedDaysAsync(e.EmployeeId, e.StartDate.Year, e.StartDate.Month, AvgBaseKind.Sick, ct);
        return true;
    }

    /// <summary>
    /// Відпустка/компенсація: база = Σ підписаних нарахувань за 12 міс (компенсація має власну колонку
    /// довідника), дні бази = 365 − відпустки за свій рахунок. Неоплачувані типи виплат не дають → false.
    /// </summary>
    public async Task<bool> FillVacationAsync(Vacation e, CancellationToken ct = default)
    {
        if (e.BaseCalculationMode != CalcMode.Auto)
            return false;
        if (e.VacationType == VacationType.Unpaid || e.VacationType == VacationType.ChildCare)
            return false;
        var kind = e.VacationType == VacationType.Compensation ? AvgBaseKind.Compensation : AvgBaseKind.Vacation;
        var b = await builder.BuildAmountAsync(e.EmployeeId, e.StartDate.Year, e.StartDate.Month, kind, ct);
        if (b.SignedMonths < FullWindowMonths)
            return false;
        var excluded = await builder.BuildExcludedDaysAsync(e.EmployeeId, e.StartDate.Year, e.StartDate.Month, kind, ct);
        e.BaseAmount = b.Amount;
        e.BaseDays = CalendarYearDays - excluded;
        return true;
    }

    /// <summary>
    /// Курси: база = Σ підписаних нарахувань за 2 міс, дні бази = робочі дні цих 2 міс із work_calendar.
    /// Заповнює і повертає true, лише якщо підписано обидва місяці.
    /// </summary>
    public async Task<bool> FillTrainingAsync(TrainingLeave e, CancellationToken ct = default)
    {
        if (e.BaseCalculationMode != CalcMode.Auto)
            return false;
        var b = await builder.BuildAmountAsync(e.EmployeeId, e.StartDate.Year, e.StartDate.Month, AvgBaseKind.Training, ct);
        if (b.SignedMonths < TrainingWindowMonths)
            return false;
        e.BaseAmount = b.Amount;
        e.BaseWorkingDays = await builder.BuildTrainingWorkingDaysAsync(e.StartDate.Year, e.StartDate.Month, ct);
        return true;
    }
}
