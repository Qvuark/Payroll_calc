using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.API.Application.AvgSalary;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// CRUD курсів підвищення кваліфікації працівника (КМУ №100). Сума рахується одразу (eager).
/// База — виплати за 2 місяці до курсів, знаменник у робочих днях. Manual-режим: база з вводу.
/// Route nested під працівника: /api/employees/{employeeId}/training-leaves.
/// </summary>
[ApiController]
[Route("api/employees/{employeeId}/training-leaves")]
public class TrainingLeavesController(AppDbContext db, AvgSalaryService avg) : ControllerBase
{
    /// <summary>
    /// Список курсів працівника, новіші зверху.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrainingLeave>>> GetAll(int employeeId)
    {
        return await db.TrainingLeaves
            .Where(t => t.EmployeeId == employeeId)
            .OrderByDescending(t => t.StartDate)
            .ToListAsync();
    }
    /// <summary>
    /// Створює курси і одразу рахує суму.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TrainingLeave>> Create(int employeeId, CreateTrainingLeaveRequest request)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == employeeId))
            return NotFound($"Працівник з id={employeeId} не знайдений.");
        var error = Validate(request);
        if (error != null)
            return BadRequest(error);
        var training = new TrainingLeave
        {
            EmployeeId = employeeId,
            BaseCalculationMode = CalcMode.Manual,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            WorkingDaysAbsent = request.WorkingDaysAbsent,
            BaseAmount = request.BaseAmount,
            BaseWorkingDays = request.BaseWorkingDays,
            InstitutionName = request.InstitutionName,
            Notes = request.Notes
        };
        avg.ApplyTraining(training);
        db.TrainingLeaves.Add(training);
        await db.SaveChangesAsync();
        return Ok(training);
    }
    /// <summary>
    /// Оновлює курси і перераховує суму.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TrainingLeave>> Update(int employeeId, int id, CreateTrainingLeaveRequest request)
    {
        var training = await db.TrainingLeaves.FirstOrDefaultAsync(t => t.Id == id && t.EmployeeId == employeeId);
        if (training == null)
            return NotFound($"Курси з id={id} не належать працівнику.");
        var error = Validate(request);
        if (error != null)
            return BadRequest(error);
        training.StartDate = request.StartDate;
        training.EndDate = request.EndDate;
        training.WorkingDaysAbsent = request.WorkingDaysAbsent;
        training.BaseAmount = request.BaseAmount;
        training.BaseWorkingDays = request.BaseWorkingDays;
        training.InstitutionName = request.InstitutionName;
        training.Notes = request.Notes;
        avg.ApplyTraining(training);
        await db.SaveChangesAsync();
        return Ok(training);
    }
    /// <summary>
    /// Фізично видаляє курси (внесли помилково).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var training = await db.TrainingLeaves.FirstOrDefaultAsync(t => t.Id == id && t.EmployeeId == employeeId);
        if (training == null)
            return NotFound($"Курси з id={id} не належать працівнику.");
        db.TrainingLeaves.Remove(training);
        await db.SaveChangesAsync();
        return NoContent();
    }
    /// <summary>
    /// Перевірка вхідних чисел. base_working_days &gt; 0 критично — інакше ділення на нуль.
    /// </summary>
    private static string? Validate(CreateTrainingLeaveRequest r)
    {
        if (r.BaseAmount <= 0)
            return "База має бути більшою за 0.";
        if (r.BaseWorkingDays <= 0)
            return "Кількість робочих днів бази має бути більшою за 0.";
        if (r.WorkingDaysAbsent <= 0)
            return "Кількість робочих днів на курсах має бути більшою за 0.";
        return null;
    }
}
public record CreateTrainingLeaveRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    int WorkingDaysAbsent,
    decimal BaseAmount,
    int BaseWorkingDays,
    string? InstitutionName = null,
    string? Notes = null);
