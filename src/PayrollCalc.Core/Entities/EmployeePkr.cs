namespace PayrollCalc.Core.Entities;

public class EmployeePkr
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal PkrHours { get; set; } = decimal.Zero;
    public int TariffGradeId { get; set; }
    public TariffGrade? TariffGrade { get; set; }
}