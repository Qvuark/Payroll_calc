using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities.Enums;
namespace PayrollCalc.Core.DTOs.Employee.Requests;

public class CreateEmployeeRequest
{
  [Required][MaxLength(50)]  public string TabNumber { get; set; } = string.Empty;
  [Required][MaxLength(200)] public string FullName { get; set; } = string.Empty;
  [Required] public DateOnly HireDate { get; set; }
  [Required] public int PositionId { get; set; }
  public string? Education { get; set; }
  public int PedExperienceYears { get; set; } = 0;
  public int? TitleTypeId { get; set; }

  [Required] public EmployeeBaseRequest Base { get; set; } = null!;
  public EmployeeWorkloadRequest? Workload { get; set; }
  public EmployeeAllowancesRequest? Allowances { get; set; }
  public EmployeeAdminRequest? Admin { get; set; }
  public EmployeeGpdRequest? Gpd { get; set; }
  public EmployeePkrRequest? Pkr { get; set; }
  public EmployeeNonPedagogicalRequest? NonPedagogical { get; set; }
}