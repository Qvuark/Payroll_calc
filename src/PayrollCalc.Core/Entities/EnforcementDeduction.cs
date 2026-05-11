namespace PayrollCalc.Core.Entities;

public class EnforcementDeduction
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; } = decimal.Zero;
    public bool IsActive { get; set; } = true;
}