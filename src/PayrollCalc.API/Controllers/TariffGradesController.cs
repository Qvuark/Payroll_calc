using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TariffGradesController : ControllerBase
{
    private readonly AppDbContext _context;

    public TariffGradesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TariffGrade>>> Get()
    {
        return await _context.TariffGrades.OrderBy(t => t.Grade).ToListAsync();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] TariffGradeRequest request)
    {
        var tariffGrade = await _context.TariffGrades.FirstOrDefaultAsync(t => t.Id == id);
        if (tariffGrade == null)
            return NotFound();

        tariffGrade.MonthlyRate = request.MonthlyRate;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
public record TariffGradeRequest(decimal MonthlyRate);
