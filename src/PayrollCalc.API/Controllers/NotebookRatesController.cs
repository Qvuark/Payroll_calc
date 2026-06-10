using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Довідник відсотків перевірки зошитів за предметами. Read-only: UI заповнює
/// дропдаун «Предмет» у блоці навантаження. Правки ставок — через seed/міграцію.
/// </summary>
[ApiController]
[Route("api/notebookrates")]
public class NotebookRatesController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotebookRatesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotebookRate>>> Get()
    {
        return await _context.NotebookRates.OrderBy(r => r.SubjectKeyword).ToListAsync();
    }
}
