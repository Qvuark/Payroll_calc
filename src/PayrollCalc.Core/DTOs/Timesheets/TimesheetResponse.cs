using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Timesheets;

/// <summary>
/// Табель працівника за місяць для читання. Несе ідентифікацію (ПІБ, таб.номер)
/// разом зі значеннями — щоб UI намалював сітку місяця без додаткових запитів.
/// </summary>
public class TimesheetResponse
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal WorkedDays { get; set; } = decimal.Zero;
    public decimal WorkedHours { get; set; } = decimal.Zero;
    public decimal NightHours { get; set; } = decimal.Zero;
    public decimal ReplacementHours { get; set; } = decimal.Zero;
    public decimal HolidayAmount { get; set; } = decimal.Zero;
    public decimal Advance { get; set; } = decimal.Zero;
    public decimal EnforcementOrders { get; set; } = decimal.Zero;
    public decimal AnnualBonus { get; set; } = decimal.Zero;
    public decimal Bonus { get; set; } = decimal.Zero;
    public decimal SickEmployer { get; set; } = decimal.Zero;
    public decimal SickFss { get; set; } = decimal.Zero;
    public decimal Vacation { get; set; } = decimal.Zero;
    public decimal Recalculation { get; set; } = decimal.Zero;
    public decimal OtherManual { get; set; } = decimal.Zero;
    /// <summary>
    /// Маппінг entity → DTO. Потребує завантаженого Employee (ПІБ, таб.номер).
    /// </summary>
    /// <param name="entity">Timesheet з Include(Employee).</param>
    /// <returns>DTO для читання.</returns>
    public static TimesheetResponse FromEntity(Timesheet entity)
    {
        return new TimesheetResponse
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TabNumber = entity.Employee?.TabNumber ?? string.Empty,
            FullName = entity.Employee?.FullName ?? string.Empty,
            Year = entity.Year,
            Month = entity.Month,
            WorkedDays = entity.WorkedDays,
            WorkedHours = entity.WorkedHours,
            NightHours = entity.NightHours,
            ReplacementHours = entity.ReplacementHours,
            HolidayAmount = entity.HolidayAmount,
            Advance = entity.Advance,
            EnforcementOrders = entity.EnforcementOrders,
            AnnualBonus = entity.AnnualBonus,
            Bonus = entity.Bonus,
            SickEmployer = entity.SickEmployer,
            SickFss = entity.SickFss,
            Vacation = entity.Vacation,
            Recalculation = entity.Recalculation,
            OtherManual = entity.OtherManual
        };
    }
    /// <summary>
    /// Порожній табель (нулі) для працівника без запису за місяць.
    /// У списку активні без рядка показуються нулями, щоб бухгалтер їх заповнив.
    /// </summary>
    /// <param name="employee">Працівник — джерело ПІБ, таб.номера, Id.</param>
    /// <param name="year">Рік періоду.</param>
    /// <param name="month">Місяць періоду.</param>
    /// <returns>DTO з нульовими значеннями.</returns>
    public static TimesheetResponse Empty(Employee employee, int year, int month)
    {
        return new TimesheetResponse
        {
            Id = 0,
            EmployeeId = employee.Id,
            TabNumber = employee.TabNumber,
            FullName = employee.FullName,
            Year = year,
            Month = month
        };
    }
}
