using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee.Requests;
public class EmployeeBaseRequest
{
    [Range(1, int.MaxValue)] public int TariffGradeId { get; set; }
    [Range(0.25, 2.0)] public decimal RateCount { get; set; } = 1.0m;

    public static EmployeeBase FromRequest(EmployeeBaseRequest request)
    {
        return new EmployeeBase
        {
            TariffGradeId = request.TariffGradeId,
            RateCount = request.RateCount
        };
    }
}