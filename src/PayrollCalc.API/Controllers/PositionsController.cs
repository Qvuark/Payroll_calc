using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{

    private readonly AppDbContext _context;

    public PositionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Position>>> GetAll()
    {
        return await _context.Positions.Include(p => p.Department).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Position>> Create([FromBody]PositionRequest request)
    {
        var position = new Position
        {
            Name = request.Name,
            DepartmentId = request.DepartmentId,
            WorkerClass = request.WorkerClass
        };

        _context.Positions.Add(position);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = position.Id }, position);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] PositionRequest request)
    {
        var position = await _context.Positions.FirstOrDefaultAsync(p => p.Id == id);
        if (position == null)
            return NotFound();
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId);
        if (department == null)
            return BadRequest("Відділ не знайдено.");
        position.Name = request.Name;
        position.DepartmentId = request.DepartmentId;
        position.WorkerClass = request.WorkerClass;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var position = await _context.Positions.FirstOrDefaultAsync(p => p.Id == id);
        if (position == null)
            return NotFound();

        if (await _context.EmployeePositions.AnyAsync(e => e.PositionId == id))
            return BadRequest("Cannot delete position that is assigned to employees.");

        _context.Positions.Remove(position);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
public record PositionRequest(string Name, int DepartmentId, WorkerClass WorkerClass);