using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.API.Application.AvgSalary;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// CRUD відпусток і компенсацій працівника (КМУ №100). Сума рахується одразу (eager).
/// Неоплачувані типи (без збереження, по догляду) зберігаються, але виплати не дають.
/// Route nested під працівника: /api/employees/{employeeId}/vacations.
/// </summary>
[ApiController]
[Route("api/employees/{employeeId}/vacations")]
public class VacationsController(AppDbContext db, AvgSalaryService avg) : ControllerBase
{
    /// <summary>
    /// Список відпусток працівника, новіші зверху.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Vacation>>> GetAll(int employeeId)
    {
        return await db.Vacations
            .Where(v => v.EmployeeId == employeeId)
            .OrderByDescending(v => v.StartDate)
            .ToListAsync();
    }
    /// <summary>
    /// Створює відпустку і одразу рахує суму (для оплачуваних типів).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Vacation>> Create(int employeeId, CreateVacationRequest request)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == employeeId))
            return NotFound($"Працівник з id={employeeId} не знайдений.");
        var error = Validate(request);
        if (error != null)
            return BadRequest(error);
        var vacation = new Vacation
        {
            EmployeeId = employeeId,
            BaseCalculationMode = CalcMode.Manual,
            VacationType = request.VacationType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CalendarDays = request.CalendarDays,
            WorkingDaysAbsent = request.WorkingDaysAbsent,
            BaseAmount = request.BaseAmount,
            BaseDays = request.BaseDays,
            OrderNumber = request.OrderNumber,
            Notes = request.Notes
        };
        avg.ApplyVacation(vacation);
        db.Vacations.Add(vacation);
        await db.SaveChangesAsync();
        return Ok(vacation);
    }
    /// <summary>
    /// Оновлює відпустку і перераховує суму.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<Vacation>> Update(int employeeId, int id, CreateVacationRequest request)
    {
        var vacation = await db.Vacations.FirstOrDefaultAsync(v => v.Id == id && v.EmployeeId == employeeId);
        if (vacation == null)
            return NotFound($"Відпустка з id={id} не належить працівнику.");
        var error = Validate(request);
        if (error != null)
            return BadRequest(error);
        vacation.VacationType = request.VacationType;
        vacation.StartDate = request.StartDate;
        vacation.EndDate = request.EndDate;
        vacation.CalendarDays = request.CalendarDays;
        vacation.WorkingDaysAbsent = request.WorkingDaysAbsent;
        vacation.BaseAmount = request.BaseAmount;
        vacation.BaseDays = request.BaseDays;
        vacation.OrderNumber = request.OrderNumber;
        vacation.Notes = request.Notes;
        avg.ApplyVacation(vacation);
        await db.SaveChangesAsync();
        return Ok(vacation);
    }
    /// <summary>
    /// Фізично видаляє відпустку (внесли помилково).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var vacation = await db.Vacations.FirstOrDefaultAsync(v => v.Id == id && v.EmployeeId == employeeId);
        if (vacation == null)
            return NotFound($"Відпустка з id={id} не належить працівнику.");
        db.Vacations.Remove(vacation);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Перевірка вхідних чисел. Неоплачувані типи бази не потребують — для них перевіряємо
    /// лише календарні дні. Для оплачуваних база і дні бази обовʼязкові (інакше нема з чого рахувати).
    /// </summary>
    private static string? Validate(CreateVacationRequest r)
    {
        if (r.CalendarDays <= 0)
            return "Кількість календарних днів відпустки має бути більшою за 0.";
        if (r.VacationType == VacationType.Unpaid || r.VacationType == VacationType.ChildCare)
            return null;
        if (r.BaseAmount is not > 0)
            return "Для оплачуваної відпустки база має бути більшою за 0.";
        if (r.BaseDays is not > 0)
            return "Для оплачуваної відпустки кількість днів бази має бути більшою за 0.";
        return null;
    }
}
public record CreateVacationRequest(
    VacationType VacationType,
    DateOnly StartDate,
    DateOnly EndDate,
    int CalendarDays,
    int WorkingDaysAbsent,
    decimal? BaseAmount = null,
    int? BaseDays = null,
    string? OrderNumber = null,
    string? Notes = null);
