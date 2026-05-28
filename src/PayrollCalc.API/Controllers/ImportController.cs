using Microsoft.AspNetCore.Mvc;
using PayrollCalc.API.Application.Import;
using PayrollCalc.Documents.Export;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Staff;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Bulk-імпорт довідників з xlsx + видача порожніх шаблонів.
/// Паралельний шлях до ручного CRUD (EmployeesController / EmployeePositionsController) — обидва пишуть у ті самі таблиці.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImportController(StaffImporter staffImporter, TemplateGenerator templateGenerator) : ControllerBase
{
    /// <summary>
    /// Імпорт staff.xlsx — масове створення/оновлення Employee + EmployeePosition.
    /// Атомарність: одна транзакція на файл, будь-яка помилка БД → відкат усього.
    /// Парсерські помилки рядків не валять імпорт — вони повертаються у ImportReport.Errors, валідні рядки зберігаються.
    /// </summary>
    /// <param name="file">xlsx за схемою StaffColumnMap (28 колонок, header row 0, дані з row 2).</param>
    /// <param name="ct">Cancellation з боку клієнта (фронт закрив запит).</param>
    /// <returns>200 + ImportReport (Created/Updated/Skipped + помилки); 400 якщо файл порожній.</returns>
    [HttpPost("staff")]
    public async Task<ActionResult<ImportReport>> ImportStaff(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Файл порожній або не передано");

        await using var stream = file.OpenReadStream();
        var report = await staffImporter.ImportAsync(stream, ct);
        return Ok(report);
    }

    /// <summary>
    /// Віддає порожній xlsx-шаблон staff: bold-заголовки (англ., ключі парсера) + рядок укр.описів для мами.
    /// Мама заповнює і завантажує назад через POST /api/import/staff.
    /// </summary>
    /// <returns>200 + xlsx attachment (staff_template.xlsx).</returns>
    [HttpGet("templates/staff")]
    public ActionResult GetStaffTemplate()
    {
        var bytes = templateGenerator.Generate(new StaffColumnMap(),
            "Staff");
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "staff_template.xlsx");
    }
}