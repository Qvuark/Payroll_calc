using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeeGpdDto
{
    public int EmployeeId { get; set; }
    public int TariffGradeId { get; set; }
    public decimal GpdHours { get; set; }
    public static EmployeeGpdDto FromEntity(EmployeeGpd e)
    {
        return new EmployeeGpdDto()
        {
            EmployeeId = e.EmployeeId,
            TariffGradeId = e.TariffGradeId,
            GpdHours = e.GpdHours
        };
    }
}