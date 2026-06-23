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
public class VacationsController(AppDbContext db, AvgSalaryService avg, AvgBaseAutoFiller filler) : ControllerBase
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
        var error = Validate(request, request.BaseCalculationMode);
        if (error != null)
            return BadRequest(error);
        var vacation = new Vacation
        {
            EmployeeId = employeeId,
            BaseCalculationMode = request.BaseCalculationMode,
            VacationType = request.VacationType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CalendarDays = request.CalendarDays,
            WorkingDaysAbsent = request.WorkingDaysAbsent,
            BaseAmount = request.BaseAmount,
            BaseDays = request.BaseDays,
            OverrideTotalAmount = request.OverrideTotalAmount,
            OrderNumber = request.OrderNumber,
            Notes = request.Notes
        };
        if (await filler.FillVacationAsync(vacation) is false && RequiresAutoBase(vacation))
            return BadRequest(InsufficientHistory);
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
        var error = Validate(request, request.BaseCalculationMode);
        if (error != null)
            return BadRequest(error);
        vacation.BaseCalculationMode = request.BaseCalculationMode;
        vacation.VacationType = request.VacationType;
        vacation.StartDate = request.StartDate;
        vacation.EndDate = request.EndDate;
        vacation.CalendarDays = request.CalendarDays;
        vacation.WorkingDaysAbsent = request.WorkingDaysAbsent;
        vacation.BaseAmount = request.BaseAmount;
        vacation.BaseDays = request.BaseDays;
        vacation.OverrideTotalAmount = request.OverrideTotalAmount;
        vacation.OrderNumber = request.OrderNumber;
        vacation.Notes = request.Notes;
        if (await filler.FillVacationAsync(vacation) is false && RequiresAutoBase(vacation))
            return BadRequest(InsufficientHistory);
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

    private const string InsufficientHistory =
        "Недостатньо підписаної історії (потрібно 12 місяців) для авто-розрахунку. Перемкніть на ручний режим і введіть базу.";

    /// <summary>
    /// Чи треба для цієї відпустки авто-база: лише оплачувані типи в режимі Auto.
    /// Неоплачувані (без збереження, по догляду) виплат не дають — база їм не потрібна.
    /// </summary>
    private static bool RequiresAutoBase(Vacation v) =>
        v.BaseCalculationMode == CalcMode.Auto
        && v.VacationType != VacationType.Unpaid
        && v.VacationType != VacationType.ChildCare;

    /// <summary>
    /// Перевірка вхідних чисел. Неоплачувані типи бази не потребують. У режимі Auto база й дні
    /// приходять з історії — для оплачуваних їх не перевіряємо (заповнить filler).
    /// </summary>
    private static string? Validate(CreateVacationRequest r, CalcMode mode)
    {
        if (r.CalendarDays <= 0)
            return "Кількість календарних днів відпустки має бути більшою за 0.";
        if (r.OverrideTotalAmount < 0)
            return "Ручна сума не може бути відʼємною.";
        if (r.VacationType == VacationType.Unpaid || r.VacationType == VacationType.ChildCare)
            return null;
        if (mode == CalcMode.Manual)
        {
            if (r.BaseAmount is not > 0)
                return "Для оплачуваної відпустки база має бути більшою за 0.";
            if (r.BaseDays is not > 0)
                return "Для оплачуваної відпустки кількість днів бази має бути більшою за 0.";
        }
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
    decimal? OverrideTotalAmount = null,
    string? OrderNumber = null,
    string? Notes = null,
    CalcMode BaseCalculationMode = CalcMode.Manual);
