using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Employee.Requests;

public class UpdateEmployeeRequest
{
    [MaxLength(200)] public string? FullName { get; set; }
    [MaxLength(200)] public string? Education { get; set; }
    public WorkerClass? WorkerClass { get; set; }
    public int? PositionId { get; set; }
    public int? TitleTypeId { get; set; }
    public int? PedExperienceYears { get; set; }
    public EmployeeStatus? Status { get; set; }

    public EmployeeBaseRequest? Base { get; set; }
    public EmployeeWorkloadRequest? Workload { get; set; }
    public EmployeeAllowancesRequest? Allowances { get; set; }
    public EmployeeAdminRequest? Admin { get; set; }
    public EmployeeGpdRequest? Gpd { get; set; }
    public EmployeePkrRequest? Pkr { get; set; }
    public EmployeeNonPedagogicalRequest? NonPedagogical { get; set; }
}
