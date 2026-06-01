using Microsoft.AspNetCore.Mvc;
using PayrollCalc.API.Application.Import;
using PayrollCalc.Documents.Export;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Staff;
using PayrollCalc.Documents.Import.Teachers;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Bulk-імпорт довідників з xlsx + видача порожніх шаблонів.
/// Паралельний шлях до ручного CRUD (EmployeesController / EmployeePositionsController) — обидва пишуть у ті самі таблиці.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImportController(
    StaffImporter staffImporter,
    TeachersImporter teachersImporter,
    TemplateGenerator templateGenerator,
    TimesheetTemplateService timesheetTemplateService) : ControllerBase
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
    /// Імпорт teachers.xlsx — масове створення/оновлення Employee + EmployeePosition + блоки Workload/Admin.
    /// Атомарність: одна транзакція на файл, будь-яка помилка БД → відкат усього.
    /// Парсерські помилки рядків не валять імпорт — вони повертаються у ImportReport.Errors, валідні рядки зберігаються.
    /// </summary>
    /// <param name="file">xlsx за схемою TeachersColumnMap (40 колонок, header row 0, дані з row 2).</param>
    /// <param name="ct">Cancellation з боку клієнта.</param>
    /// <returns>200 + ImportReport; 400 якщо файл порожній.</returns>
    [HttpPost("teachers")]
    public async Task<ActionResult<ImportReport>> ImportTeachers(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Файл порожній або не передано");

        await using var stream = file.OpenReadStream();
        var report = await teachersImporter.ImportAsync(stream, ct);
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

    /// <summary>
    /// Віддає порожній xlsx-шаблон teachers: bold-заголовки (англ.) + рядок укр.описів для мами.
    /// </summary>
    /// <returns>200 + xlsx attachment (teachers_template.xlsx).</returns>
    [HttpGet("templates/teachers")]
    public ActionResult GetTeachersTemplate()
    {
        var bytes = templateGenerator.Generate(new TeachersColumnMap(),
            "Teachers");
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "teachers_template.xlsx");
    }

    /// <summary>
    /// Віддає pre-filled timesheet-шаблон на (year, month): рядок на кожного активного працівника
    /// з №/ІПН/таб/ПІБ/посадою. Мама вписує числа (відпрацьовано/заміна/нічні) і вантажить назад.
    /// </summary>
    /// <param name="year">Рік періоду.</param>
    /// <param name="month">Місяць періоду (1..12).</param>
    /// <param name="ct">Cancellation з боку клієнта.</param>
    /// <returns>200 + xlsx attachment (timesheet_{year}_{month}.xlsx); 400 при невалідному місяці.</returns>
    [HttpGet("templates/timesheet")]
    public async Task<ActionResult> GetTimesheetTemplate([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            return BadRequest("Місяць має бути в діапазоні 1..12");
        var bytes = await timesheetTemplateService.BuildAsync(year, month, ct);
        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"timesheet_{year}_{month:00}.xlsx");
    }
}