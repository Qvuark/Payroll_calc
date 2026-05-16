using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DepartmentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Department>>> GetAll()
    {
        return await _db.Departments.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Department>> Create([FromBody]DepartmentRequest request)
    {
        var department = new Department { Name = request.Name };
        _db.Departments.Add(department);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { id = department.Id }, department);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody]DepartmentRequest request)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }

        department.Name = request.Name;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var department = await _db.Departments.FindAsync(id);
        if (department == null)
        {
            return NotFound();
        }
        if (await _db.Positions.AnyAsync(p => p.DepartmentId == id))
            return Conflict("Неможливо видалити підрозділ — є посади, які до нього прив'язані.");
        _db.Departments.Remove(department);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
public record DepartmentRequest(string Name);