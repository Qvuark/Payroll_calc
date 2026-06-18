using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Системні параметри розрахунку (ставки податків, МЗП, №1749...). Читання + правка
/// значення за ключем. Звідси параметри живлять рушій через PayrollParamsFactory.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SystemParamsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SystemParam>>> Get()
    {
        return await context.SystemParams.ToListAsync();
    }

    [HttpPut("{key}")]
    public async Task<ActionResult> Update(string key, [FromBody] SystemParamRequest request)
    {
        // Від'ємна ставка податку/надбавки отруїла б усі наступні розрахунки.
        if (request.Value < 0)
            return BadRequest("Значення параметра не може бути від'ємним.");
        var systemParam = await context.SystemParams.FirstOrDefaultAsync(s => s.Key == key);
        if (systemParam == null)
            return NotFound();

        systemParam.Value = request.Value;

        await context.SaveChangesAsync();
        return NoContent();
    }
}

public record SystemParamRequest(decimal Value);