using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Довідник тарифних розрядів. Лише читання + правка окладу (MonthlyRate):
/// набір розрядів фіксований тарифною сіткою, рядки не додаються й не видаляються.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TariffGradesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TariffGrade>>> Get()
    {
        return await context.TariffGrades.OrderBy(t => t.Grade).ToListAsync();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] TariffGradeRequest request)
    {
        // Нульовий/від'ємний оклад розряду зламав би розрахунок усім, хто на ньому сидить.
        if (request.MonthlyRate <= 0)
            return BadRequest("Оклад розряду має бути більшим за нуль.");
        var tariffGrade = await context.TariffGrades.FirstOrDefaultAsync(t => t.Id == id);
        if (tariffGrade == null)
            return NotFound();

        tariffGrade.MonthlyRate = request.MonthlyRate;

        await context.SaveChangesAsync();
        return NoContent();
    }
}
public record TariffGradeRequest(decimal MonthlyRate);
