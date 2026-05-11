namespace PayrollCalc.Core.Entities;

public class EmployeeAdmin
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal DirectorPct { get; set; } = decimal.Zero;
    public decimal AdminRateCount { get; set; } = decimal.Zero;
    public decimal PedRateCount { get; set; } = decimal.Zero;
}