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
    public async Task<ActionResult<IEnumerable<WorkCalendar>>> Get()
    {
        return await _context.WorkCalendars
            .OrderBy(wc => wc.Month)
            .ToListAsync();
    }
}