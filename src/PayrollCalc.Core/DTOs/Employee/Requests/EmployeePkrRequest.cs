using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee.Requests;

public class EmployeePkrRequest
{
    [Range(0.0, 40.0)] public decimal PkrHours { get; set; }
    [Range(1, int.MaxValue)] public int TariffGradeId { get; set; }

    public static EmployeePkr FromRequest(EmployeePkrRequest request)
    {
        return new EmployeePkr
        {
            PkrHours = request.PkrHours,
            TariffGradeId = request.TariffGradeId
        };
    }
}
