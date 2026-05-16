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
}