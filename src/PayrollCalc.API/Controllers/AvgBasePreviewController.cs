using Microsoft.AspNetCore.Mvc;
using PayrollCalc.API.Application.AvgSalary;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Прев'ю авто-бази середньоденної: чи вистачає підписаної історії для події і скільки вийде.
/// Дзеркалить підрахунок автозаповнення, але нічого не пише — форма підсвічує стан ще до збереження.
/// </summary>
[ApiController]
[Route("api/employees/{employeeId:int}/avg-base-preview")]
public class AvgBasePreviewController(AvgBaseBuilder builder) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AvgBasePreview>> Get(int employeeId, int year, int month, string kind, CancellationToken ct)
    {
        AvgBaseKind baseKind;
        switch (kind)
        {
            case "sick":
                baseKind = AvgBaseKind.Sick;
                break;
            case "vacation":
                baseKind = AvgBaseKind.Vacation;
                break;
            case "training":
                baseKind = AvgBaseKind.Training;
                break;
            default:
                return BadRequest("Невідомий вид події.");
        }
        // Курси беруть 2 місяці історії, решта подій — 12.
        var required = baseKind == AvgBaseKind.Training ? 2 : 12;
        var built = await builder.BuildAmountAsync(employeeId, year, month, baseKind, ct);
        return new AvgBasePreview(built.SignedMonths, required, built.SignedMonths >= required, built.Amount);
    }
}
public record AvgBasePreview(int SignedMonths, int RequiredMonths, bool Enough, decimal Amount);
