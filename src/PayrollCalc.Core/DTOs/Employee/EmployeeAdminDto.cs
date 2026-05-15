using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeeAdminDto
{
    public int EmployeeId { get; set; }
    public decimal DirectorPct { get; set; } = decimal.Zero;
    public decimal AdminRateCount { get; set; } = decimal.Zero;
    public decimal PedRateCount { get; set; } = decimal.Zero;
    public static EmployeeAdminDto FromEntity(EmployeeAdmin e)
    {
        return new EmployeeAdminDto()
        {
            EmployeeId = e.EmployeeId,
            DirectorPct = e.DirectorPct,
            AdminRateCount = e.AdminRateCount,
            PedRateCount = e.PedRateCount
        };
    }
}