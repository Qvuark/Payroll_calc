namespace PayrollCalc.Core.Entities;

public class NotebookRate
{
    public int Id { get; set; }
    public string SubjectKeyword { get; set; } = string.Empty;
    public decimal Pct { get; set; } = decimal.Zero;
}