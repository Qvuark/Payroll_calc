using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Employee.Requests;

public class EmployeeGpdRequest
{
    [Range(0.0, 30.0)] public decimal GpdHours { get; set; }
    [Range(1, int.MaxValue)] public int TariffGradeId { get; set; }

    public static EmployeeGpd FromRequest(EmployeeGpdRequest request)
    {
        return new EmployeeGpd
        {
            GpdHours = request.GpdHours,
            TariffGradeId = request.TariffGradeId
        };
    }
}
