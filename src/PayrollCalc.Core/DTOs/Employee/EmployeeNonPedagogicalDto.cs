using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeeNonPedagogicalDto
{
    public int EmployeeId { get; set; }
    public bool HasDisinfectants { get; set; } = false;
    public bool HasNightShifts { get; set; } = false;
    public static EmployeeNonPedagogicalDto FromEntity(EmployeeNonPedagogical e)
    {
        return new EmployeeNonPedagogicalDto()
        {
            EmployeeId = e.EmployeeId,
            HasDisinfectants = e.HasDisinfectants,
            HasNightShifts = e.HasNightShifts
        };
    }
}