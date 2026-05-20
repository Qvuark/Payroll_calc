using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeeGpdDto
{
    public int EmployeePositionId { get; set; }
    public int TariffGradeId { get; set; }
    public decimal GpdHours { get; set; }
    public static EmployeeGpdDto FromEntity(EmployeeGpd e)
    {
        return new EmployeeGpdDto()
        {
            EmployeePositionId = e.EmployeePositionId,
            TariffGradeId = e.TariffGradeId,
            GpdHours = e.GpdHours
        };
    }
}