using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeePkrDto
{
    public int EmployeePositionId { get; set; }
    public int TariffGradeId { get; set; }
    public decimal PkrHours { get; set; }
    public static EmployeePkrDto FromEntity(EmployeePkr e)
    {
        return new EmployeePkrDto()
        {
            EmployeePositionId = e.EmployeePositionId,
            TariffGradeId = e.TariffGradeId,
            PkrHours = e.PkrHours
        };
    }
}