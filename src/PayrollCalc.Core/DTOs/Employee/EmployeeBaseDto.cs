using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeeBaseDto
{
    public int EmployeeId { get; set; }
    public int TariffGradeId { get; set; }
    public decimal RateCount { get; set; } = 1.0m;
    public static EmployeeBaseDto FromEntity(EmployeeBase e)
    {
        return new EmployeeBaseDto()
        {
            EmployeeId = e.EmployeeId,
            TariffGradeId = e.TariffGradeId,
            RateCount = e.RateCount
        };
    }
}