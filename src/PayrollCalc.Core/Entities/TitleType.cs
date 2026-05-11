namespace PayrollCalc.Core.Entities;

public class TitleType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Pct { get; set; } = decimal.Zero;
}