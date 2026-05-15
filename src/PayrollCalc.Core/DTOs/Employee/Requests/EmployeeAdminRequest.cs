using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee.Requests;

public class EmployeeAdminRequest
{
    [Range(0.0, 1.0)] public decimal DirectorPct { get; set; }
    [Range(0.0, 2.0)] public decimal AdminRateCount { get; set; }
    [Range(0.0, 2.0)] public decimal PedRateCount { get; set; }

    public static EmployeeAdmin FromRequest(EmployeeAdminRequest request)
    {
        return new EmployeeAdmin
        {
            DirectorPct = request.DirectorPct,
            AdminRateCount = request.AdminRateCount,
            PedRateCount = request.PedRateCount
        };
    }
}