using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;
using EmployeeEntity = PayrollCalc.Core.Entities.Employee;
public class EmployeeDetailDto
{
    public int Id { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public DateOnly? DismissalDate { get; set; }
    public string? Education { get; set; }
    public int PedExperienceYears { get; set; }
    public WorkerClass WorkerClass { get; set; }
    public EmployeeStatus Status { get; set; }
    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public int? TitleTypeId { get; set; }
    public string? TitleTypeName { get; set; }
    public EmployeeBaseDto? Base { get; set; }
    public EmployeeWorkloadDto? Workload { get; set; }
    public EmployeeAllowancesDto? Allowances { get; set; }
    public EmployeeAdminDto? Admin { get; set; }
    public EmployeeGpdDto? Gpd { get; set; }
    public EmployeePkrDto? Pkr { get; set; }
    public EmployeeNonPedagogicalDto? NonPedagogical { get; set; }
    static public EmployeeDetailDto FromEntity(EmployeeEntity e)
    {
        var dto = new EmployeeDetailDto()
        {
            Id=e.Id,
            TabNumber = e.TabNumber,
            FullName = e.FullName,
            HireDate= e.HireDate,
            DismissalDate = e.DismissalDate,
            Education= e.Education,
            PedExperienceYears = e.PedExperienceYears,
            Status = e.Status,
            PositionId = e.PositionId,
            PositionName=e.Position?.Name??string.Empty,
            TitleTypeId = e.TitleTypeId,
            TitleTypeName=e.TitleType?.Name??string.Empty,
            Base=e.Base!=null?EmployeeBaseDto.FromEntity(e.Base):null,
            Admin=e.Admin!=null?EmployeeAdminDto.FromEntity(e.Admin):null,
            Allowances=e.Allowances!=null?EmployeeAllowancesDto.FromEntity(e.Allowances):null,
            Gpd=e.Gpd!=null?EmployeeGpdDto.FromEntity(e.Gpd):null,
            Pkr=e.Pkr!=null?EmployeePkrDto.FromEntity(e.Pkr):null,
            Workload=e.Workload!=null?EmployeeWorkloadDto.FromEntity(e.Workload):null,
            NonPedagogical=e.NonPedagogical!=null?EmployeeNonPedagogicalDto.FromEntity(e.NonPedagogical):null,
        };
        return dto;
    }
    
}