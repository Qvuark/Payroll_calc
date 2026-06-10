using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkCalendarController : ControllerBase
{
    private readonly AppDbContext _context;

    public WorkCalendarController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkCalendar>>> Get([FromQuery] int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        return await _context.WorkCalendars
            .Where(wc => wc.Year == targetYear)
            .OrderBy(wc => wc.Month)
            .ToListAsync();
    }

    /// <summary>
    /// Створює календар на рік: 12 місяців з нульовою нормою, бухгалтер далі
    /// проставляє дні через PUT. Сід заповнює лише поточний рік — наступні додаються тут.
    /// </summary>
    /// <param name="year">Рік з route.</param>
    /// <returns>201 зі створеними місяцями, 400 на кривий рік, 409 якщо рік уже існує.</returns>
    [HttpPost("{year:int}")]
    public async Task<ActionResult<IEnumerable<WorkCalendar>>> CreateYear(int year)
    {
        if (year is < 2020 or > 2100)
            return BadRequest("Рік має бути в діапазоні 2020..2100.");
        if (await _context.WorkCalendars.AnyAsync(wc => wc.Year == year))
            return Conflict($"Календар на {year} рік уже існує.");
        var months = Enumerable.Range(1, 12)
            .Select(m => new WorkCalendar { Year = year, Month = m, WorkDays = 0 })
            .ToList();
        _context.WorkCalendars.AddRange(months);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { year }, months);
    }

    /// <summary>
    /// Оновлює норму робочих днів місяця. Місяць із збереженими розрахунками
    /// закритий: зміна норми заднім числом розійшлася б з виданою відомістю.
    /// </summary>
    /// <param name="year">Рік з route.</param>
    /// <param name="month">Місяць з route (1..12).</param>
    /// <param name="request">Нова норма днів (0..31).</param>
    /// <returns>204 NoContent, 400 на криві дані, 404 якщо місяця нема, 409 якщо місяць закритий.</returns>
    [HttpPut("{year:int}/{month:int}")]
    public async Task<ActionResult> Update(int year, int month, [FromBody] WorkCalendarRequest request)
    {
        if (month is < 1 or > 12)
            return BadRequest("Місяць має бути в діапазоні 1..12.");
        if (request.WorkDays is < 0 or > 31)
            return BadRequest("Норма робочих днів має бути в діапазоні 0..31.");
        var entry = await _context.WorkCalendars
            .FirstOrDefaultAsync(wc => wc.Year == year && wc.Month == month);
        if (entry == null)
            return NotFound($"Місяць {month:00}.{year} відсутній у календарі. Спершу створіть рік.");
        if (await _context.Calculations.AnyAsync(c => c.Year == year && c.Month == month))
            return Conflict("Місяць закритий — за нього вже є збережені розрахунки.");
        entry.WorkDays = request.WorkDays;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record WorkCalendarRequest(int WorkDays);
