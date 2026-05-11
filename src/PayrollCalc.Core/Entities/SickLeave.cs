using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

public class SickLeave
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.MinValue;
    public DateOnly EndDate { get; set; } = DateOnly.MinValue;
    public int DaysTotal { get; set; } = 0;
    public int DaysEmployer { get; set; } = 0;
    public int DaysFss { get; set; } = 0;
    public int InsuranceSeniorityYrs { get; set; } = 0;
    public decimal PaymentPct { get; set; } = decimal.Zero;
    public CalcMode BaseCalculationMode { get; set; } = CalcMode.Auto;
    public decimal BaseAmount { get; set; } = decimal.Zero;
    public int BaseExcludedDays { get; set; } = 0;
    public int BaseDays { get; set; } = 0;
    public decimal AverageDaily { get; set; } = decimal.Zero;
    public decimal AmountEmployer { get; set; } = decimal.Zero;
    public decimal AmountFss { get; set; } = decimal.Zero;
    public decimal TotalAmount { get; set; } = decimal.Zero;
    public string? EfssNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}