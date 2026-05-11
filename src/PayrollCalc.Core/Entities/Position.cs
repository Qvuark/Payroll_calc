using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public WorkerClass WorkerClass { get; set; }
    public Department? Department { get; set; }
}