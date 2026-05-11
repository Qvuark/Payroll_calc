namespace PayrollCalc.Core.Entities;

public class TariffGrade
{
    public int Id { get; set; }
    public int Grade { get; set; } = 0;
    public decimal MonthlyRate { get; set; } = decimal.Zero;
    public DateOnly EffectiveDate { get; set; } = DateOnly.MinValue;
}