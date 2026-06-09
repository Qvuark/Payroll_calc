namespace PayrollCalc.Core.Entities;

public class Timesheet
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal WorkedDays { get; set; } = decimal.Zero;
    /// <summary>
    /// Відпрацьовані години за місяць (відомість D) — для погодинних посад (сторож). 0 у денних.
    /// </summary>
    public decimal WorkedHours { get; set; } = decimal.Zero;
    public decimal NightHours { get; set; } = decimal.Zero;
    public decimal HolidayAmount { get; set; } = decimal.Zero;
    public decimal ReplacementHours { get; set; } = decimal.Zero;
    public decimal Recalculation { get; set; } = decimal.Zero;
    public decimal Advance { get; set; } = decimal.Zero;
    public decimal EnforcementOrders { get; set; } = decimal.Zero;
    public decimal AnnualBonus { get; set; } = decimal.Zero;
    public decimal OtherManual { get; set; } = decimal.Zero;
}