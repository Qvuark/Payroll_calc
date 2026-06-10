using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Довідник звань (Старший вчитель, Методист...). Read-only: UI заповнює дропдаун
/// і сам фільтрує за WorkerClass ставки. Правки звань — через seed/міграцію.
/// </summary>
[ApiController]
[Route("api/titletypes")]
public class TitleTypesController : ControllerBase
{
    private readonly AppDbContext _context;

    public TitleTypesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TitleType>>> Get()
    {
        return await _context.TitleTypes.OrderBy(t => t.Name).ToListAsync();
    }
}
