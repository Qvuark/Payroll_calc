using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;
using EmployeeEntity = PayrollCalc.Core.Entities.Employee;
public class EmployeeSummaryDto
{
    public int Id { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public WorkerClass WorkerClass { get; set; }
    public EmployeeStatus Status { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public static EmployeeSummaryDto FromEntity(EmployeeEntity e)
    {
        return new EmployeeSummaryDto()
        {
            Id = e.Id,
            TabNumber = e.TabNumber,
            FullName = e.FullName,
            WorkerClass = e.WorkerClass,
            Status = e.Status,
            PositionName = e.Position?.Name ?? string.Empty,
            DepartmentName = e.Position?.Department?.Name ?? string.Empty
        };
    }
}