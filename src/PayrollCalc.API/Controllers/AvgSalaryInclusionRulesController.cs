using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Довідник «що входить у базу середньоденної». Кожна виплата має 4 галочки —
/// чи рахувати її в базу лікарняних / відпускних / курсів / компенсації.
/// Лише читання + правка галочок: набір рядків фіксований іменами рушія, рядки не додаються й не видаляються.
/// </summary>
[ApiController]
[Route("api/inclusion-rules")]
public class AvgSalaryInclusionRulesController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AvgSalaryInclusionRule>>> Get()
    {
        // Порядок Id = порядок сіду: спершу постійні виплати, потім премія, потім самі події.
        return await context.AvgSalaryInclusionRules.OrderBy(r => r.Id).ToListAsync();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] AvgSalaryInclusionRuleRequest request)
    {
        var rule = await context.AvgSalaryInclusionRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule == null)
            return NotFound();

        rule.IncludeSick = request.IncludeSick;
        rule.IncludeVacation = request.IncludeVacation;
        rule.IncludeTraining = request.IncludeTraining;
        rule.IncludeCompensation = request.IncludeCompensation;

        await context.SaveChangesAsync();
        return NoContent();
    }
}
public record AvgSalaryInclusionRuleRequest(bool IncludeSick, bool IncludeVacation, bool IncludeTraining, bool IncludeCompensation);
