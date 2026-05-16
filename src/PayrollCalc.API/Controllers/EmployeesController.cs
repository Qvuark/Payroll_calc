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
        if (await _db.Employees.AnyAsync(e => e.TabNumber == employeeRequest.TabNumber))
            return Conflict($"Працівник з табельним номером {employeeRequest.TabNumber} вже існує.");
        var position = await _db.Positions.FindAsync(employeeRequest.PositionId);
        if(position is null)
            return BadRequest("Position not found");
        if (employeeRequest.WorkerClass != position.WorkerClass)
            return BadRequest($"Клас працівника ({employeeRequest.WorkerClass}) не співпадає з класом посади «{position.Name}» ({position.WorkerClass}).");
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
            TitleTypeId = employeeRequest.TitleTypeId,
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

    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeDetailDto>> UpdateEmployee(int id, [FromBody]UpdateEmployeeRequest employeeRequest)
    {
        if(employeeRequest is null)
            return BadRequest("Employee request is null");
        var titleType = await _db.TitleTypes.FindAsync(employeeRequest.TitleTypeId);
        if(titleType is null)
            return BadRequest("Title type not found");
        var position = await _db.Positions.FindAsync(employeeRequest.PositionId);
        if(position is null)
            return BadRequest("Position not found");
        if (employeeRequest.WorkerClass != position.WorkerClass)
            return BadRequest($"Клас працівника ({employeeRequest.WorkerClass}) не співпадає з класом посади «{position.Name}» ({position.WorkerClass}).");
        var employee = await _db.Employees
        .Include(e => e.Base)
        .Include(e => e.Workload)
        .Include(e => e.Allowances)
        .Include(e => e.Admin)
        .Include(e => e.Gpd)
        .Include(e => e.Pkr)
        .Include(e => e.NonPedagogical)
        .FirstOrDefaultAsync(e => e.Id == id);
        if(employee is null)
            return NotFound();
        var errors = EmployeeValidator.ValidateBlocks(
          employeeRequest.WorkerClass,
          hasWorkload: employeeRequest.Workload != null,
          hasAdmin: employeeRequest.Admin != null,
          hasAllowances: employeeRequest.Allowances != null,
          hasNonPedagogical: employeeRequest.NonPedagogical != null);
        if (errors != null)
          return BadRequest(errors);
        employee.FullName = employeeRequest.FullName;
        employee.PedExperienceYears = employeeRequest.PedExperienceYears;
        employee.WorkerClass = employeeRequest.WorkerClass;
        employee.Status = employeeRequest.Status;
        employee.PositionId = employeeRequest.PositionId;
        employee.TitleTypeId = employeeRequest.TitleTypeId;
        employee.Education = employeeRequest.Education;
        UpsertBase(employee, employeeRequest.Base);
        UpsertAdmin(employee, employeeRequest.Admin);
        UpsertAllowances(employee, employeeRequest.Allowances);
        UpsertGpd(employee, employeeRequest.Gpd);
        UpsertPkr(employee, employeeRequest.Pkr);
        UpsertWorkload(employee, employeeRequest.Workload);
        UpsertNonPedagogical(employee, employeeRequest.NonPedagogical);
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
    private void UpsertBase(Employee employee, EmployeeBaseRequest request)
    {
        if(employee.Base == null)
        {
            employee.Base = EmployeeBaseRequest.FromRequest(request);
            return;
        }
        employee.Base.TariffGradeId = request.TariffGradeId;
        employee.Base.RateCount = request.RateCount;
    } 
    private void UpsertAdmin(Employee employee, EmployeeAdminRequest? request)
    {
        if(request == null)
        {
            if(employee.Admin != null)
            {
                _db.EmployeeAdmins.Remove(employee.Admin);
                employee.Admin = null;
            }       
            return;     
        }
        if(employee.Admin == null)
        {
            employee.Admin = EmployeeAdminRequest.FromRequest(request);
            return;
        }
        employee.Admin.DirectorPct = request.DirectorPct;
        employee.Admin.AdminRateCount = request.AdminRateCount;
        employee.Admin.PedRateCount = request.PedRateCount;
    }
    private void UpsertAllowances(Employee employee, EmployeeAllowancesRequest? request)
    {
        if(request == null)
        {
            if(employee.Allowances != null)
            {
                _db.EmployeeAllowances.Remove(employee.Allowances);
                employee.Allowances = null;
            }            
            return;
        }
        if(employee.Allowances == null)
        {
            employee.Allowances = EmployeeAllowancesRequest.FromRequest(request);
            return;
        }
        employee.Allowances.HasClassMgmt = request.HasClassMgmt;
        employee.Allowances.ClassGradeGroup = request.ClassGradeGroup;
        employee.Allowances.HasCabinet = request.HasCabinet;
        employee.Allowances.CabinetType = request.CabinetType;
        employee.Allowances.HasGym = request.HasGym;
        employee.Allowances.HasShootingRange = request.HasShootingRange;
        employee.Allowances.HasComputers = request.HasComputers;
        employee.Allowances.HasExtracurricular = request.HasExtracurricular;
        employee.Allowances.HasWebsite = request.HasWebsite;
        employee.Allowances.HasMilitaryAcct = request.HasMilitaryAcct;
        employee.Allowances.HasUnfavorable = request.HasUnfavorable;
        employee.Allowances.HasMentor = request.HasMentor;
        employee.Allowances.MentorAmount = request.MentorAmount;
        employee.Allowances.HasLibraryMgmt = request.HasLibraryMgmt;
        employee.Allowances.LibraryMgmtAmount = request.LibraryMgmtAmount;
        employee.Allowances.HasTextbooks = request.HasTextbooks;
        employee.Allowances.TextbooksAmount = request.TextbooksAmount;
    }
    private void UpsertGpd(Employee employee, EmployeeGpdRequest? request)
    {
        if(request == null)
        {
            if(employee.Gpd != null)
            {
                _db.EmployeeGpds.Remove(employee.Gpd);
                employee.Gpd = null;
            }            
            return;
        }
        if(employee.Gpd == null)
        {
            employee.Gpd = EmployeeGpdRequest.FromRequest(request);
            return;
        }
        employee.Gpd.GpdHours = request.GpdHours;
        employee.Gpd.TariffGradeId = request.TariffGradeId;
    }
    private void UpsertPkr(Employee employee, EmployeePkrRequest? request)
    {
        if(request == null)
        {
            if(employee.Pkr != null)
            {
                _db.EmployeePkrs.Remove(employee.Pkr);
                employee.Pkr = null;
            }            
            return;
        }
        if(employee.Pkr == null)
        {
            employee.Pkr = EmployeePkrRequest.FromRequest(request);
            return;
        }
        employee.Pkr.PkrHours = request.PkrHours;
        employee.Pkr.TariffGradeId = request.TariffGradeId;
    }
    private void UpsertWorkload(Employee employee, EmployeeWorkloadRequest? request)
    {
        if(request == null)
        {
            if(employee.Workload != null)
            {
                _db.EmployeeWorkloads.Remove(employee.Workload);
                employee.Workload = null;
            }            
            return;
        }
        if(employee.Workload == null)
        {
            employee.Workload = EmployeeWorkloadRequest.FromRequest(request);
            return;
        }
        employee.Workload.Hours1To4 = request.Hours1To4;
        employee.Workload.IndividualHours1To4 = request.IndividualHours1To4;
        employee.Workload.Hours5To9 = request.Hours5To9;
        employee.Workload.IndividualHours5To9 = request.IndividualHours5To9;
        employee.Workload.Hours10To11 = request.Hours10To11;
        employee.Workload.IndividualHours10To11 = request.IndividualHours10To11;
        employee.Workload.NotebookHours1To4 = request.NotebookHours1To4;
        employee.Workload.NotebookHours5To9 = request.NotebookHours5To9;
        employee.Workload.NotebookHours10To11 = request.NotebookHours10To11;
        employee.Workload.InclusiveHours1To4 = request.InclusiveHours1To4;
        employee.Workload.InclusiveHours5To9 = request.InclusiveHours5To9;
        employee.Workload.NotebookRateId = request.NotebookRateId;
    }
    private void UpsertNonPedagogical(Employee employee, EmployeeNonPedagogicalRequest? request)
    {
        if(request == null)
        {
            if(employee.NonPedagogical != null)
            {
                _db.EmployeeNonPedagogical.Remove(employee.NonPedagogical);
                employee.NonPedagogical = null;
            }            
            return;
        }
        if(employee.NonPedagogical == null)
        {
            employee.NonPedagogical = EmployeeNonPedagogicalRequest.FromRequest(request);
            return;
        }
        employee.NonPedagogical.HasDisinfectants = request.HasDisinfectants;
        employee.NonPedagogical.HasNightShifts = request.HasNightShifts;
    }
}