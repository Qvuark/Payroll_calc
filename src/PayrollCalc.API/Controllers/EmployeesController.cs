using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.DTOs.Employees.Requests;
using PayrollCalc.Infrastructure.Data;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.DTOs.Employees;
namespace PayrollCalc.API.Controllers;

/// <summary>
/// Persona-level CRUD працівника. Operate тільки над полями самого Employee (ПІБ, ІПН, стаж...).
/// Ставки (EmployeePosition) і блоки надбавок — окремий EmployeePositionsController.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmployeesController(AppDbContext context) : ControllerBase
{
    /// <summary>
    /// Список активних працівників з інформацією про головну ставку.
    /// Звільнених не повертає. Для повної картки з усіма ставками — GET /{id}.
    /// </summary>
    /// <returns>Колекція EmployeeSummaryDto.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeSummaryDto>>> GetAll()
    {
        var employees = await context.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position).ThenInclude(p => p!.Department)
            .Include(e => e.Positions).ThenInclude(p => p.TariffGrade)
            .Where(e => e.Status != EmployeeStatus.Dismissed)
            .ToListAsync();
        return Ok(employees.Select(emp => EmployeeSummaryDto.FromEntity(emp)).ToList());
    }
    /// <summary>
    /// Повна картка працівника з усіма ставками і блоками надбавок.
    /// </summary>
    /// <param name="id">Id працівника.</param>
    /// <returns>EmployeeDetailDto або 404 якщо не знайдено.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDetailDto>> GetById(int id)
    {
        var employee = await context.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position).ThenInclude(p => p!.Department)
            .Include(e => e.Positions).ThenInclude(p => p.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.Workload)
            .Include(e => e.Positions).ThenInclude(p => p.Admin)
            .Include(e => e.Positions).ThenInclude(p => p.Gpd)
            .Include(e => e.Positions).ThenInclude(p => p.Pkr)
            .Include(e => e.Positions).ThenInclude(p => p.NonPedagogical)
            .Include(e => e.TitleType)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return NotFound();
        return Ok(EmployeeDetailDto.FromEntity(employee));
    }
    /// <summary>
    /// Створює нового працівника (persona). Ставки додаються окремо через
    /// POST /api/employees/{id}/positions. Status за замовчуванням Active.
    /// </summary>
    /// <param name="request">Дані persona — обов'язково TabNumber і FullName.</param>
    /// <returns>201 CreatedAtAction з EmployeeDetailDto, 409 при дублі TabNumber, 400 при невалідному TitleTypeId.</returns>
    [HttpPost]
    public async Task<ActionResult<EmployeeDetailDto>> Create(CreateEmployeeRequest request)
    {
        if (await context.Employees.AnyAsync(e => e.TaxId == request.TaxId))
            return Conflict($"Працівник з ІПН {request.TaxId} вже існує.");
        if (request.TitleTypeId.HasValue)
        {
            var titleType = await context.TitleTypes.FindAsync(request.TitleTypeId.Value);
            if (titleType == null)
                return BadRequest("Title type not found.");
        }
        var employee = CreateEmployeeRequest.FromRequest(request);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, EmployeeDetailDto.FromEntity(employee));
    }
    /// <summary>
    /// Оновлює persona-поля працівника. TabNumber і HireDate immutable.
    /// Перевіряє consistency Status/DismissalDate: звільнений — дата обов'язкова, активний — null.
    /// </summary>
    /// <param name="id">Id працівника.</param>
    /// <param name="request">Поля для оновлення (включно зі Status і DismissalDate).</param>
    /// <returns>200 з EmployeeDetailDto, 404 якщо не знайдено, 400 при невалідному стані.</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeDetailDto>> Update(int id, UpdateEmployeeRequest request)
    {
        if (request.TitleTypeId.HasValue)
        {
            var titleType = await context.TitleTypes.FindAsync(request.TitleTypeId.Value);
            if (titleType == null)
                return BadRequest("Title type not found.");
        }
        var employee = await context.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position).ThenInclude(p => p!.Department)
            .Include(e => e.Positions).ThenInclude(p => p.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.Workload)
            .Include(e => e.Positions).ThenInclude(p => p.Admin)
            .Include(e => e.Positions).ThenInclude(p => p.Gpd)
            .Include(e => e.Positions).ThenInclude(p => p.Pkr)
            .Include(e => e.Positions).ThenInclude(p => p.NonPedagogical)
            .Include(e => e.TitleType)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return NotFound();
        if (request.Status == EmployeeStatus.Dismissed && request.DismissalDate == null)
            return BadRequest("DismissalDate обов'язкова при статусі Dismissed.");
        if (request.Status != EmployeeStatus.Dismissed && request.DismissalDate != null)
            return BadRequest("DismissalDate має бути null при статусі не Dismissed.");
        if (request.TaxId != employee.TaxId && await context.Employees.AnyAsync(e => e.TaxId == request.TaxId && e.Id != id))
            return Conflict($"Працівник з ІПН {request.TaxId} вже існує.");
        employee.FullName = request.FullName;
        employee.TaxId = request.TaxId;
        employee.DismissalDate = request.DismissalDate;
        employee.Education = request.Education;
        employee.PedExperienceYears = request.PedExperienceYears;
        employee.SocialBenefitPct = request.SocialBenefitPct;
        employee.TitleTypeId = request.TitleTypeId;
        employee.Status = request.Status;
        employee.GeneralExperienceYears = request.GeneralExperienceYears;
        await context.SaveChangesAsync();
        return Ok(EmployeeDetailDto.FromEntity(employee));
    }
    /// <summary>
    /// Soft delete працівника — Status=Dismissed + DismissalDate=today.
    /// Фізично запис не видаляється (project rule: soft delete only).
    /// Окремі ставки звільняються через DELETE /api/employees/{id}/positions/{posId}.
    /// </summary>
    /// <param name="id">Id працівника.</param>
    /// <returns>204 NoContent при успіху, 404 якщо не знайдено.</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return NotFound();
        employee.Status = EmployeeStatus.Dismissed;
        employee.DismissalDate = DateOnly.FromDateTime(DateTime.Now);
        await context.SaveChangesAsync();
        return NoContent();
    }
}