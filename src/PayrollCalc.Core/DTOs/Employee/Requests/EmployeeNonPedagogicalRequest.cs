using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee.Requests;

public class EmployeeNonPedagogicalRequest
{
    public bool HasDisinfectants { get; set; }
    public bool HasNightShifts { get; set; }
    
    public static EmployeeNonPedagogical FromRequest(EmployeeNonPedagogicalRequest request)
    {
        return new EmployeeNonPedagogical
        {
            HasDisinfectants = request.HasDisinfectants,
            HasNightShifts = request.HasNightShifts
        };
    }
}
