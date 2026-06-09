using Microsoft.AspNetCore.Mvc;
using PayrollCalc.API.Application.Calculation;

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
        var results = await service.RunAllAsync(year, month, ct);
        return Ok(results);
    }
}
