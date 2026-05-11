namespace PayrollCalc.Core.Entities;

public class SystemParam
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public decimal Value { get; set; } = decimal.Zero;
    public DateOnly EffectiveDate { get; set; } = DateOnly.MinValue;
}