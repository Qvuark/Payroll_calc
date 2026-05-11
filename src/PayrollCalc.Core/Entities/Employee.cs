using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

public class Employee
{
    public int Id { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; } = DateOnly.MinValue;
    public DateOnly? DismissalDate { get; set; }
    public string? Education { get; set; }
    public int PedExperienceYears { get; set; } = 0;
    public WorkerClass WorkerClass { get; set; }
    public EmployeeStatus Status { get; set; }
    public int PositionId { get; set; }
    public Position? Position { get; set; }
    public int? TitleTypeId { get; set; }
    public TitleType? TitleType { get; set; }
    public EmployeeBase? Base { get; set; }
    public EmployeeGpd? Gpd { get; set; }
    public EmployeeWorkload? Workload { get; set; }
    public EmployeeAdmin? Admin { get; set; }
    public EmployeeAllowances? Allowances { get; set; }
    public EmployeePkr? Pkr { get; set; }
    public EmployeeNonPedagogical? NonPedagogical { get; set; }
}
