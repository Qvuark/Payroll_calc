namespace PayrollCalc.Core.Entities;

public class EmployeeBase
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int TariffGradeId { get; set; }
    public TariffGrade? TariffGrade { get; set; }
    public decimal RateCount { get; set; } = 1.0m;
}
