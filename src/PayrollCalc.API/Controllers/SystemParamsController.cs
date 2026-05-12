using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemParamsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SystemParamsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SystemParam>>> Get()
    {
        return await _context.SystemParams.ToListAsync();
    }

    [HttpPut("{key}")]
    public async Task<ActionResult> Update(string key, [FromBody] SystemParamRequest request)
    {
        var systemParam = await _context.SystemParams.FirstOrDefaultAsync(s => s.Key == key);
        if (systemParam == null)
            return NotFound();

        systemParam.Value = request.Value;

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record SystemParamRequest(decimal Value);