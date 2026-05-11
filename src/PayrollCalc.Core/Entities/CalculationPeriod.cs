namespace PayrollCalc.Core.Entities;

public class CalculationPeriod
{
    public int Id { get; set; }
    public int CalculationId { get; set; }
    public Calculation? Calculation { get; set; }
    public DateOnly DateFrom { get; set; } = DateOnly.MinValue;
    public DateOnly DateTo { get; set; } = DateOnly.MinValue;
    public int WorkDays { get; set; } = 0;
    public decimal MonthlyRate { get; set; } = decimal.Zero;
    public decimal Bonus1749Pct { get; set; } = decimal.Zero;
}