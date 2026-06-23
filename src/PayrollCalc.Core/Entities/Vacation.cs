using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

public class Vacation
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public VacationType VacationType { get; set; } = VacationType.Annual;
    public DateOnly StartDate { get; set; } = DateOnly.MinValue;
    public DateOnly EndDate { get; set; } = DateOnly.MinValue;
    public int CalendarDays { get; set; } = 0;
    public int WorkingDaysAbsent { get; set; } = 0;
    public CalcMode BaseCalculationMode { get; set; } = CalcMode.Auto;
    public decimal? BaseAmount { get; set; }
    public int? BaseDays { get; set; }
    public decimal? AverageDaily { get; set; }
    public decimal? TotalAmount { get; set; }
    /// <summary>
    /// Ручна підсумкова сума замість формули — коли факт розійшовся з розрахунком. Null = береться формула.
    /// </summary>
    public decimal? OverrideTotalAmount { get; set; }
    public bool IsCarryOver { get; set; } = false;
    public int? CarryOverYear { get; set; }
    public int? CarryOverMonth { get; set; }
    public string? OrderNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
