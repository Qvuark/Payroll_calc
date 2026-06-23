using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.API.Application.AvgSalary;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// CRUD лікарняних працівника (КМУ №1266). Сума рахується одразу при створенні/оновленні
/// (eager) — бухгалтер бачить її в UI без окремого прогону розрахунку місяця. Manual-режим:
/// база береться з вводу. Route nested під працівника: /api/employees/{employeeId}/sick-leaves.
/// </summary>
[ApiController]
[Route("api/employees/{employeeId}/sick-leaves")]
public class SickLeavesController(AppDbContext db, AvgSalaryService avg, AvgBaseAutoFiller filler) : ControllerBase
{
    /// <summary>
    /// Список лікарняних працівника, новіші зверху.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SickLeave>>> GetAll(int employeeId)
    {
        return await db.SickLeaves
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync();
    }
    /// <summary>
    /// Створює лікарняний і одразу рахує суму. PaymentPct у запиті 0 → виводиться зі стажу.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<SickLeave>> Create(int employeeId, CreateSickLeaveRequest request)
    {
        if (!await db.Employees.AnyAsync(e => e.Id == employeeId))
            return NotFound($"Працівник з id={employeeId} не знайдений.");
        var error = Validate(request, request.BaseCalculationMode);
        if (error != null)
            return BadRequest(error);
        var sickLeave = new SickLeave
        {
            EmployeeId = employeeId,
            BaseCalculationMode = request.BaseCalculationMode,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DaysTotal = request.DaysTotal,
            WorkingDaysAbsent = request.WorkingDaysAbsent,
            BaseAmount = request.BaseAmount,
            BaseExcludedDays = request.BaseExcludedDays,
            InsuranceSeniorityYrs = request.InsuranceSeniorityYrs,
            PaymentPct = request.PaymentPct,
            OverrideAmountEmployer = request.OverrideAmountEmployer,
            OverrideAmountFss = request.OverrideAmountFss,
            EfssNumber = request.EfssNumber,
            Notes = request.Notes
        };
        if (await filler.FillSickAsync(sickLeave) is false && sickLeave.BaseCalculationMode == CalcMode.Auto)
            return BadRequest(InsufficientHistory);
        avg.ApplySick(sickLeave);
        db.SickLeaves.Add(sickLeave);
        await db.SaveChangesAsync();
        return Ok(sickLeave);
    }
    /// <summary>
    /// Оновлює лікарняний і перераховує суму.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<SickLeave>> Update(int employeeId, int id, CreateSickLeaveRequest request)
    {
        var sickLeave = await db.SickLeaves.FirstOrDefaultAsync(s => s.Id == id && s.EmployeeId == employeeId);
        if (sickLeave == null)
            return NotFound($"Лікарняний з id={id} не належить працівнику.");
        var error = Validate(request, request.BaseCalculationMode);
        if (error != null)
            return BadRequest(error);
        sickLeave.BaseCalculationMode = request.BaseCalculationMode;
        sickLeave.StartDate = request.StartDate;
        sickLeave.EndDate = request.EndDate;
        sickLeave.DaysTotal = request.DaysTotal;
        sickLeave.WorkingDaysAbsent = request.WorkingDaysAbsent;
        sickLeave.BaseAmount = request.BaseAmount;
        sickLeave.BaseExcludedDays = request.BaseExcludedDays;
        sickLeave.InsuranceSeniorityYrs = request.InsuranceSeniorityYrs;
        sickLeave.PaymentPct = request.PaymentPct;
        sickLeave.OverrideAmountEmployer = request.OverrideAmountEmployer;
        sickLeave.OverrideAmountFss = request.OverrideAmountFss;
        sickLeave.EfssNumber = request.EfssNumber;
        sickLeave.Notes = request.Notes;
        if (await filler.FillSickAsync(sickLeave) is false && sickLeave.BaseCalculationMode == CalcMode.Auto)
            return BadRequest(InsufficientHistory);
        avg.ApplySick(sickLeave);
        await db.SaveChangesAsync();
        return Ok(sickLeave);
    }
    /// <summary>
    /// Фізично видаляє лікарняний (внесли помилково). Soft-delete тут не потрібен — подія не людина.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int employeeId, int id)
    {
        var sickLeave = await db.SickLeaves.FirstOrDefaultAsync(s => s.Id == id && s.EmployeeId == employeeId);
        if (sickLeave == null)
            return NotFound($"Лікарняний з id={id} не належить працівнику.");
        db.SickLeaves.Remove(sickLeave);
        await db.SaveChangesAsync();
        return NoContent();
    }
    private const string InsufficientHistory =
        "Недостатньо підписаної історії (потрібно 12 місяців) для авто-розрахунку. Перемкніть на ручний режим і введіть базу.";

    /// <summary>
    /// Перевірка вхідних чисел. Повертає текст помилки українською або null, якщо все валідне.
    /// У режимі Auto база й виключені дні приходять з історії — їх не перевіряємо (excl &lt; 365
    /// критично лише для ручного вводу, інакше ділення на нуль у середньоденній).
    /// </summary>
    private static string? Validate(CreateSickLeaveRequest r, CalcMode mode)
    {
        if (mode == CalcMode.Manual)
        {
            if (r.BaseAmount <= 0)
                return "База має бути більшою за 0.";
            if (r.BaseExcludedDays < 0 || r.BaseExcludedDays >= 365)
                return "Виключених днів має бути від 0 до 364.";
        }
        if (r.DaysTotal <= 0)
            return "Кількість днів хвороби має бути більшою за 0.";
        if (r.WorkingDaysAbsent < 0)
            return "Робочих днів пропущено не може бути відʼємним.";
        if (r.WorkingDaysAbsent > r.DaysTotal)
            return "Робочих днів пропущено не може бути більше за календарні дні хвороби.";
        if (r.InsuranceSeniorityYrs < 0)
            return "Страховий стаж не може бути відʼємним.";
        if (r.PaymentPct < 0)
            return "Відсоток виплати не може бути відʼємним.";
        if (r.OverrideAmountEmployer < 0 || r.OverrideAmountFss < 0)
            return "Ручна сума не може бути відʼємною.";
        return null;
    }
}
public record CreateSickLeaveRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    int DaysTotal,
    int WorkingDaysAbsent,
    decimal BaseAmount,
    int BaseExcludedDays,
    int InsuranceSeniorityYrs,
    decimal PaymentPct = 0m,
    decimal? OverrideAmountEmployer = null,
    decimal? OverrideAmountFss = null,
    string? EfssNumber = null,
    string? Notes = null,
    CalcMode BaseCalculationMode = CalcMode.Manual);
