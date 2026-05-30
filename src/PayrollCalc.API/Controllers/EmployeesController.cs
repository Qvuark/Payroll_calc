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
            .Include(e => e.Positions).ThenInclude(p => p.TitleType)
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
    /// <returns>201 CreatedAtAction з EmployeeDetailDto, 409 при дублі ІПН.</returns>
    [HttpPost]
    public async Task<ActionResult<EmployeeDetailDto>> Create(CreateEmployeeRequest request)
    {
        if (await context.Employees.AnyAsync(e => e.TaxId == request.TaxId))
            return Conflict($"Працівник з ІПН {request.TaxId} вже існує.");
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
        var employee = await context.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position).ThenInclude(p => p!.Department)
            .Include(e => e.Positions).ThenInclude(p => p.TariffGrade)
            .Include(e => e.Positions).ThenInclude(p => p.Workload)
            .Include(e => e.Positions).ThenInclude(p => p.Admin)
            .Include(e => e.Positions).ThenInclude(p => p.Gpd)
            .Include(e => e.Positions).ThenInclude(p => p.Pkr)
            .Include(e => e.Positions).ThenInclude(p => p.NonPedagogical)
            .Include(e => e.Positions).ThenInclude(p => p.TitleType)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return NotFound();
        if (request.Status == EmployeeStatus.Dismissed && request.DismissalDate == null)
            return BadRequest("DismissalDate обов'язкова при статусі Dismissed.");
        if (request.Status != EmployeeStatus.Dismissed && request.DismissalDate != null)
            return BadRequest("DismissalDate має бути null при статусі не Dismissed.");
        if (request.TaxId != employee.TaxId && await context.Employees.AnyAsync(e => e.TaxId == request.TaxId && e.Id != id))
            return Conflict($"Працівник з ІПН {request.TaxId} вже існує.");
        if (!request.IsHonored && request.HonoredAmount != null)
            return BadRequest("HonoredAmount має бути null коли IsHonored=false.");
        employee.FullName = request.FullName;
        employee.TaxId = request.TaxId;
        employee.DismissalDate = request.DismissalDate;
        employee.Education = request.Education;
        employee.PedExperienceYears = request.PedExperienceYears;
        employee.SocialBenefitPct = request.SocialBenefitPct;
        employee.Status = request.Status;
        employee.GeneralExperienceYears = request.GeneralExperienceYears;
        employee.IsHonored = request.IsHonored;
        employee.HonoredAmount = request.HonoredAmount;
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
        var employee = await context.Employees
            .Include(e => e.Positions)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (employee == null)
            return NotFound();
        var today = DateOnly.FromDateTime(DateTime.Now);
        employee.Status = EmployeeStatus.Dismissed;
        employee.DismissalDate = today;
        // Каскадно звільняємо всі активні ставки. Інакше вони лишаються "висіти" активними:
        // розрахунок Phase 5 нарахує зарплату звільненому, а унікальний індекс ставки
        // (WHERE DismissalDate IS NULL) заблокує повторне прийняття людини на ту саму посаду.
        foreach (var position in employee.Positions.Where(p => p.DismissalDate == null))
        {
            position.DismissalDate = today;
            position.IsPrimary = false;
        }
        await context.SaveChangesAsync();
        return NoContent();
    }
}