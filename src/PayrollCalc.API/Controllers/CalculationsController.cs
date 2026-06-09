using Microsoft.AspNetCore.Mvc;
using PayrollCalc.API.Application.Calculation;
using PayrollCalc.Documents.Export.Payslip;
using PayrollCalc.Documents.Export.Vedomost;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Запуск розрахунку зарплати. Збирає вхід із БД → рушій → повертає повний розклад (CalcResult)
/// і зберігає зведення у Calculation. Повний покомпонентний результат потрібен для звірки з еталоном.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CalculationsController(PayrollCalculationService service) : ControllerBase
{
    /// <summary>
    /// Рахує одного працівника за місяць. 404 якщо працівника немає.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Calculate([FromQuery] int employeeId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (ValidatePeriod(year, month) is { } error)
            return error;
        var result = await service.RunAsync(employeeId, year, month, ct);
        return result is null
            ? NotFound($"Працівника #{employeeId} не знайдено.")
            : Ok(result);
    }

    /// <summary>
    /// Рахує всіх активних працівників за місяць (для відомості).
    /// </summary>
    [HttpPost("all")]
    public async Task<IActionResult> CalculateAll([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (ValidatePeriod(year, month) is { } error)
            return error;
        var results = await service.RunAllAsync(year, month, ct);
        return Ok(results);
    }

    /// <summary>
    /// Відомість (xlsx) за місяць: рахує всіх активних і віддає файл як еталонний бланк.
    /// </summary>
    [HttpGet("vedomost")]
    public async Task<IActionResult> Vedomost([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (ValidatePeriod(year, month) is { } error)
            return error;
        var results = await service.RunAllAsync(year, month, ct);
        var bytes = new VedomostExporter().Build(results, year, month);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"vedomost_{year}_{month:00}.xlsx");
    }

    /// <summary>
    /// Розрахункові листи (xlsx) за місяць: по дві платіжки в ряд для всіх активних.
    /// </summary>
    [HttpGet("payslips")]
    public async Task<IActionResult> Payslips([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (ValidatePeriod(year, month) is { } error)
            return error;
        var results = await service.RunAllAsync(year, month, ct);
        var bytes = new PayslipExporter().Build(results, year, month);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"payslips_{year}_{month:00}.xlsx");
    }

    /// <summary>
    /// Спільна перевірка періоду: невалідний місяць інакше дійшов би до назв місяців
    /// (IndexOutOfRange) чи конструктора дати і повернувся 500 замість 400.
    /// </summary>
    private BadRequestObjectResult? ValidatePeriod(int year, int month)
    {
        if (month is < 1 or > 12)
            return BadRequest("Місяць має бути в діапазоні 1..12");
        if (year is < 2020 or > 2100)
            return BadRequest("Рік має бути в діапазоні 2020..2100");
        return null;
    }
}
