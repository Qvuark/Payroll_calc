using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.DTOs.Timesheets;
using PayrollCalc.Core.DTOs.Timesheets.Requests;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TimesheetsController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Табелі всіх активних працівників за місяць. Хто без запису — повертається з нулями,
    /// щоб бухгалтер їх заповнив (список будується від працівників, не від наявних табелів).
    /// </summary>
    /// <param name="year">Рік періоду.</param>
    /// <param name="month">Місяць періоду (1..12).</param>
    /// <returns>Список табелів, по одному на активного працівника.</returns>
    [HttpGet]
    public async Task<ActionResult<List<TimesheetResponse>>> Get([FromQuery] int year, [FromQuery] int month)
    {
        if (month is < 1 or > 12)
            return BadRequest("Місяць має бути в діапазоні 1..12");
        var employees = await db.Employees
            .Where(e => e.Status != EmployeeStatus.Dismissed)
            .OrderBy(e => e.FullName)
            .ToListAsync();
        var sheets = await db.Timesheets
            .Include(t => t.Employee)
            .Where(t => t.Year == year && t.Month == month)
            .ToDictionaryAsync(t => t.EmployeeId);
        return employees
            .Select(e => sheets.TryGetValue(e.Id, out var ts)
                ? TimesheetResponse.FromEntity(ts)
                : TimesheetResponse.Empty(e, year, month))
            .ToList();
    }
    /// <summary>
    /// Табель одного працівника за місяць. Якщо запису нема, але працівник існує —
    /// повертаються нулі (не 404). 404 лише коли працівника взагалі не існує.
    /// </summary>
    /// <param name="employeeId">Id працівника.</param>
    /// <param name="year">Рік періоду.</param>
    /// <param name="month">Місяць періоду.</param>
    /// <returns>Табель працівника або нулі.</returns>
    [HttpGet("{employeeId:int}")]
    public async Task<ActionResult<TimesheetResponse>> GetOne(int employeeId, [FromQuery] int year, [FromQuery] int month)
    {
        var employee = await db.Employees.FindAsync(employeeId);
        if (employee is null)
            return NotFound($"Працівника {employeeId} не знайдено");
        var sheet = await db.Timesheets
            .Include(t => t.Employee)
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.Year == year && t.Month == month);
        return sheet is not null
            ? TimesheetResponse.FromEntity(sheet)
            : TimesheetResponse.Empty(employee, year, month);
    }
    /// <summary>
    /// Запис або оновлення табеля за натуральним ключем (EmployeeId, Year, Month).
    /// Нема запису — вставка, є — оновлення тих самих полів. Ідемпотентно.
    /// </summary>
    /// <param name="request">Дані табеля.</param>
    /// <returns>Збережений табель.</returns>
    [HttpPut]
    public async Task<ActionResult<TimesheetResponse>> Upsert([FromBody] TimesheetRequest request)
    {
        var employee = await db.Employees.FindAsync(request.EmployeeId);
        if (employee is null)
            return NotFound($"Працівника {request.EmployeeId} не знайдено");
        var calendar = await db.WorkCalendars
            .FirstOrDefaultAsync(wc => wc.Year == request.Year && wc.Month == request.Month);
        if (calendar is null)
            return BadRequest($"Немає робочого календаря за {request.Month:00}.{request.Year}");
        if (request.WorkedDays > calendar.WorkDays)
            return BadRequest($"Відпрацьовано днів ({request.WorkedDays}) більше норми місяця ({calendar.WorkDays})");
        var timesheet = await db.Timesheets.FirstOrDefaultAsync(t =>
            t.EmployeeId == request.EmployeeId && t.Year == request.Year && t.Month == request.Month);
        if (timesheet is null)
        {
            timesheet = TimesheetRequest.FromRequest(request);
            db.Timesheets.Add(timesheet);
        }
        else
        {
            request.ApplyTo(timesheet);
        }
        await db.SaveChangesAsync();

        timesheet.Employee = employee;
        return TimesheetResponse.FromEntity(timesheet);
    }
}

