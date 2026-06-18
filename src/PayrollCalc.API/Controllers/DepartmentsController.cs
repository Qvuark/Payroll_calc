using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// CRUD довідника відділів школи. Delete заблоковано, якщо на відділ є посади
/// (інакше осиротіли б посади з цим DepartmentId).
/// </summary>
[ApiController]
[Route("api/departments")]
public class DepartmentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetAll()
    {
        return await db.Departments.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Department>> Create([FromBody]DepartmentRequest request)
    {
        var department = new Department { Name = request.Name };
        db.Departments.Add(department);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = department.Id }, department);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody]DepartmentRequest request)
    {
        var department = await db.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }

        department.Name = request.Name;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await db.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }
        if (await db.Positions.AnyAsync(p => p.DepartmentId == id))
            return Conflict("Неможливо видалити підрозділ — є посади, які до нього прив'язані.");
        db.Departments.Remove(department);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
public record DepartmentRequest(string Name);