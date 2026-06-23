using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

public class TrainingLeave
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.MinValue;
    public DateOnly EndDate { get; set; } = DateOnly.MinValue;
    public int WorkingDaysAbsent { get; set; } = 0;
    public CalcMode BaseCalculationMode { get; set; } = CalcMode.Auto;
    public decimal BaseAmount { get; set; } = decimal.Zero;
    public int BaseWorkingDays { get; set; } = 0;
    public decimal AverageDaily { get; set; } = decimal.Zero;
    public decimal TotalAmount { get; set; } = decimal.Zero;
    /// <summary>
    /// Ручна підсумкова сума замість формули — коли факт розійшовся з розрахунком. Null = береться формула.
    /// </summary>
    public decimal? OverrideTotalAmount { get; set; }
    public string? InstitutionName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}