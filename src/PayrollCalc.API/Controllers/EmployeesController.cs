using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.DTOs.Employee;
using PayrollCalc.Core.DTOs.Employee.Requests;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;
using PayrollCalc.Core.Validators;
namespace PayrollCalc.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _db;
    public EmployeesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeSummaryDto>>> GetAll()
    {
        var employees = await _db.Employees
        .Include(e=>e.Position)
        .ThenInclude(p=>p!.Department)
        .Where(e=>e.Status != EmployeeStatus.Dismissed)
        .ToListAsync();
        var dtos = employees.Select(e => EmployeeSummaryDto.FromEntity(e));
        return Ok(dtos);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(int id)
    {
        var employee = await _db.Employees
            .Include(e => e.Base)
            .Include(e => e.Workload)
            .Include(e => e.Allowances)
            .Include(e => e.Admin)
            .Include(e => e.Gpd)
            .Include(e => e.Pkr)
            .Include(e => e.NonPedagogical)
            .Include(e => e.Position)
            .Include(e => e.TitleType)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return NotFound();
        var dto = EmployeeDetailDto.FromEntity(employee);
        return Ok(dto);
    }
    [HttpPost]
    public async Task<ActionResult<EmployeeDetailDto>> CreateEmployee([FromBody]CreateEmployeeRequest employeeRequest)
    {
        if(employeeRequest is null)
            return BadRequest("Employee request is null");
        var position = await _db.Positions.FindAsync(employeeRequest.PositionId);
        if(position is null)
            return BadRequest("Position not found");
        TitleType? titleType = null;
        if(employeeRequest.TitleTypeId.HasValue)
        {
            titleType = await _db.TitleTypes.FindAsync(employeeRequest.TitleTypeId);
            if(titleType is null)
                return BadRequest("Title type not found");
        }
        var errors = EmployeeValidator.ValidateBlocks(
            employeeRequest.WorkerClass,
            hasWorkload: employeeRequest.Workload != null,
            hasAdmin: employeeRequest.Admin != null,
            hasAllowances: employeeRequest.Allowances != null,
            hasNonPedagogical: employeeRequest.NonPedagogical != null
        );
        if (errors != null)
            return BadRequest(errors);
        var employee = new Employee
        {
            TabNumber = employeeRequest.TabNumber,
            FullName = employeeRequest.FullName,
            HireDate= employeeRequest.HireDate,
            Education= employeeRequest.Education,
            PedExperienceYears = employeeRequest.PedExperienceYears,
            WorkerClass = employeeRequest.WorkerClass,
            Status = EmployeeStatus.Active,
            PositionId = employeeRequest.PositionId,
            Position=position,
            TitleTypeId = employeeRequest.TitleTypeId,
            TitleType=titleType,
            Base=EmployeeBaseRequest.FromRequest(employeeRequest.Base),
            Admin = employeeRequest.Admin != null ? EmployeeAdminRequest.FromRequest(employeeRequest.Admin) : null,
            Allowances = employeeRequest.Allowances != null ? EmployeeAllowancesRequest.FromRequest(employeeRequest.Allowances) : null,
            Gpd = employeeRequest.Gpd != null ? EmployeeGpdRequest.FromRequest(employeeRequest.Gpd) : null,
            Pkr = employeeRequest.Pkr != null ? EmployeePkrRequest.FromRequest(employeeRequest.Pkr) : null,
            Workload = employeeRequest.Workload != null ? EmployeeWorkloadRequest.FromRequest(employeeRequest.Workload) : null,
            NonPedagogical = employeeRequest.NonPedagogical != null ? EmployeeNonPedagogicalRequest.FromRequest(employeeRequest.NonPedagogical) : null,
        };
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, EmployeeDetailDto.FromEntity(employee));
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<EmployeeDetailDto>> UpdateEmployee(int id, [FromBody]UpdateEmployeeRequest employeeRequest)
    {
        if(employeeRequest is null)
            return BadRequest("Employee request is null");
        TitleType? titleType = null;
        Position? position = null;
        if(employeeRequest.PositionId.HasValue)
        {
            position = await _db.Positions.FindAsync(employeeRequest.PositionId);
            if(position is null)
                return BadRequest("Position not found");
        }
        if(employeeRequest.TitleTypeId.HasValue)
        {
            titleType = await _db.TitleTypes.FindAsync(employeeRequest.TitleTypeId);
            if(titleType is null)
                return BadRequest("Title type not found");
        }
        var employee = await _db.Employees
        .Include(e => e.Base)
        .Include(e => e.Workload)
        .Include(e => e.Allowances)
        .Include(e => e.Admin)
        .Include(e => e.Gpd)
        .Include(e => e.Pkr)
        .Include(e => e.NonPedagogical)
        .Include(e => e.Position)
        .Include(e => e.TitleType)
        .FirstOrDefaultAsync(e => e.Id == id);
        if(employee is null)
            return NotFound();
        var effectiveClass = employeeRequest.WorkerClass ?? employee.WorkerClass;
        var errors = EmployeeValidator.ValidateBlocks(
            effectiveClass,
            hasWorkload: employeeRequest.Workload != null || employee.Workload != null,
            hasAdmin: employeeRequest.Admin != null || employee.Admin != null,
            hasAllowances: employeeRequest.Allowances != null || employee.Allowances != null,
            hasNonPedagogical: employeeRequest.NonPedagogical != null || employee.NonPedagogical != null);
        if (errors != null)
            return BadRequest(errors);
        if(employeeRequest.FullName is not null)
            employee.FullName = employeeRequest.FullName;
        if(employeeRequest.Education is not null)
            employee.Education= employeeRequest.Education;
        if(employeeRequest.PedExperienceYears is not null)
            employee.PedExperienceYears = employeeRequest.PedExperienceYears.Value;
        if(employeeRequest.WorkerClass is not null)
            employee.WorkerClass = employeeRequest.WorkerClass.Value;
        if(employeeRequest.Status is not null)
            employee.Status = employeeRequest.Status.Value;
        if(employeeRequest.PositionId is not null)
        {
            employee.PositionId = employeeRequest.PositionId.Value;
            employee.Position=position;
        }
        if(employeeRequest.TitleTypeId.HasValue)
        {
            employee.TitleTypeId = employeeRequest.TitleTypeId.Value;
            employee.TitleType=titleType;
        }
        if(employeeRequest.Base != null)
        {
            if(employee.Base != null)
            {
                employee.Base.TariffGradeId = employeeRequest.Base.TariffGradeId;
                employee.Base.RateCount = employeeRequest.Base.RateCount;
            }
            else
            {
                employee.Base = EmployeeBaseRequest.FromRequest(employeeRequest.Base);
            }
        }          
        if(employeeRequest.Admin != null)
        {
            if(employee.Admin != null)
            {
                employee.Admin.DirectorPct = employeeRequest.Admin.DirectorPct;
                employee.Admin.AdminRateCount = employeeRequest.Admin.AdminRateCount;
                employee.Admin.PedRateCount = employeeRequest.Admin.PedRateCount;
            }
            else
            {
                employee.Admin = EmployeeAdminRequest.FromRequest(employeeRequest.Admin);
            }
        }
        if(employeeRequest.Allowances != null)
        {
            if(employee.Allowances != null)
            {
                employee.Allowances.HasClassMgmt = employeeRequest.Allowances.HasClassMgmt;
                employee.Allowances.ClassGradeGroup = employeeRequest.Allowances.ClassGradeGroup;
                employee.Allowances.HasCabinet = employeeRequest.Allowances.HasCabinet;
                employee.Allowances.CabinetType = employeeRequest.Allowances.CabinetType;
                employee.Allowances.HasGym = employeeRequest.Allowances.HasGym;
                employee.Allowances.HasShootingRange = employeeRequest.Allowances.HasShootingRange;
                employee.Allowances.HasComputers = employeeRequest.Allowances.HasComputers;
                employee.Allowances.HasExtracurricular = employeeRequest.Allowances.HasExtracurricular;
                employee.Allowances.HasWebsite = employeeRequest.Allowances.HasWebsite;
                employee.Allowances.HasMilitaryAcct = employeeRequest.Allowances.HasMilitaryAcct;
                employee.Allowances.HasUnfavorable = employeeRequest.Allowances.HasUnfavorable;
                employee.Allowances.HasMentor = employeeRequest.Allowances.HasMentor;
                employee.Allowances.MentorAmount = employeeRequest.Allowances.MentorAmount;
                employee.Allowances.HasLibraryMgmt = employeeRequest.Allowances.HasLibraryMgmt;
                employee.Allowances.LibraryMgmtAmount = employeeRequest.Allowances.LibraryMgmtAmount;
                employee.Allowances.HasTextbooks = employeeRequest.Allowances.HasTextbooks;
                employee.Allowances.TextbooksAmount = employeeRequest.Allowances.TextbooksAmount;
            }
            else
            {
                employee.Allowances = EmployeeAllowancesRequest.FromRequest(employeeRequest.Allowances);
            }
        }
        if(employeeRequest.Gpd != null)
        {
            if(employee.Gpd != null)
            {
                employee.Gpd.GpdHours = employeeRequest.Gpd.GpdHours;
                employee.Gpd.TariffGradeId = employeeRequest.Gpd.TariffGradeId;
            }
            else
            {
                employee.Gpd = EmployeeGpdRequest.FromRequest(employeeRequest.Gpd);
            }
        }
        if(employeeRequest.Pkr != null)
        {
            if(employee.Pkr != null)
            {
                employee.Pkr.PkrHours = employeeRequest.Pkr.PkrHours;
                employee.Pkr.TariffGradeId = employeeRequest.Pkr.TariffGradeId;
            }
            else
            {
                employee.Pkr = EmployeePkrRequest.FromRequest(employeeRequest.Pkr);
            }
        }
        if(employeeRequest.Workload != null)
        {
            if(employee.Workload != null)
            {
                employee.Workload.Hours1To4 = employeeRequest.Workload.Hours1To4;
                employee.Workload.IndividualHours1To4 = employeeRequest.Workload.IndividualHours1To4;
                employee.Workload.Hours5To9 = employeeRequest.Workload.Hours5To9;
                employee.Workload.IndividualHours5To9 = employeeRequest.Workload.IndividualHours5To9;
                employee.Workload.Hours10To11 = employeeRequest.Workload.Hours10To11;
                employee.Workload.IndividualHours10To11 = employeeRequest.Workload.IndividualHours10To11;
                employee.Workload.NotebookHours1To4 = employeeRequest.Workload.NotebookHours1To4;
                employee.Workload.NotebookHours5To9 = employeeRequest.Workload.NotebookHours5To9;
                employee.Workload.NotebookHours10To11 = employeeRequest.Workload.NotebookHours10To11;
                employee.Workload.InclusiveHours1To4 = employeeRequest.Workload.InclusiveHours1To4;
                employee.Workload.InclusiveHours5To9 = employeeRequest.Workload.InclusiveHours5To9;
                employee.Workload.NotebookRateId = employeeRequest.Workload.NotebookRateId;
            }
            else
            {
                employee.Workload = EmployeeWorkloadRequest.FromRequest(employeeRequest.Workload);
            }
        }
        if(employeeRequest.NonPedagogical != null)
        {
            if(employee.NonPedagogical != null)
            {
                employee.NonPedagogical.HasDisinfectants = employeeRequest.NonPedagogical.HasDisinfectants;
                employee.NonPedagogical.HasNightShifts = employeeRequest.NonPedagogical.HasNightShifts;
            }
            else
            {
                employee.NonPedagogical = EmployeeNonPedagogicalRequest.FromRequest(employeeRequest.NonPedagogical);
            }
        }
        await _db.SaveChangesAsync();
        return Ok(EmployeeDetailDto.FromEntity(employee));
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEmployee(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if(employee is null)
            return NotFound();
        if(employee.Status == EmployeeStatus.Dismissed)
            return BadRequest("Employee is already dismissed");
        employee.Status = EmployeeStatus.Dismissed;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}