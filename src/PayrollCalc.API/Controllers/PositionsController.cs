using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;
/// <summary>
/// CRUD довідника посад. Delete заблоковано, якщо на посаду призначені працівники.
/// WorkerClass посади визначає набір дозволених блоків надбавок на ставці.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PositionsController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Position>>> GetAll()
    {
        return await context.Positions.Include(p => p.Department).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Position>> Create([FromBody]PositionRequest request)
    {
        var position = new Position
        {
            Name = request.Name,
            DepartmentId = request.DepartmentId,
            WorkerClass = request.WorkerClass,
            IsHourly = request.IsHourly
        };

        context.Positions.Add(position);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = position.Id }, position);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] PositionRequest request)
    {
        var position = await context.Positions.FirstOrDefaultAsync(p => p.Id == id);
        if (position == null)
            return NotFound();
        var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId);
        if (department == null)
            return BadRequest("Відділ не знайдено.");
        position.Name = request.Name;
        position.DepartmentId = request.DepartmentId;
        position.WorkerClass = request.WorkerClass;
        position.IsHourly = request.IsHourly;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var position = await context.Positions.FirstOrDefaultAsync(p => p.Id == id);
        if (position == null)
            return NotFound();

        if (await context.EmployeePositions.AnyAsync(e => e.PositionId == id))
            return BadRequest("Неможливо видалити посаду — на неї призначені працівники.");

        context.Positions.Remove(position);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
public record PositionRequest(string Name, int DepartmentId, WorkerClass WorkerClass, bool IsHourly = false);