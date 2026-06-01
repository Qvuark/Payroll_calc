using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Timesheet;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Match-or-update Timesheet по (EmployeeId, Year, Month). TaxId резолвиться в Employee;
/// не знайдено — рядок у Errors, людину НЕ створюємо (табель посилається на існуючих).
/// Пише ЛИШЕ 3 поля вводу (WorkedDays/ReplacementHours/NightHours) — гроші-one-offs з CRUD не чіпає.
/// НЕ викликає SaveChangesAsync — Importer комітить весь файл однією транзакцією.
/// </summary>
public class TimesheetUpserter(AppDbContext db)
{
    /// <summary>
    /// Резолвить Employee за TaxId і upsert Timesheet на (year, month). Помилки (не знайдено,
    /// перевищення норми) додає у errors і повертає (null, false) = skip.
    /// </summary>
    /// <param name="row">Розпарсений рядок шаблону.</param>
    /// <param name="year">Рік періоду (з POST-параметра, не з файлу).</param>
    /// <param name="month">Місяць періоду.</param>
    /// <param name="workDaysNorm">Норма робочих днів місяця (WorkCalendar) для cross-check.</param>
    /// <param name="errors">Акумулятор помилок резолву — піде у звіт.</param>
    /// <param name="ct">Cancellation з боку клієнта.</param>
    /// <returns>(entity, wasCreated): entity=null → skip; wasCreated=true → insert; false+entity → update.</returns>
    public async Task<(Timesheet? entity, bool wasCreated)> UpsertAsync(
        TimesheetRowDto row,
        int year,
        int month,
        int workDaysNorm,
        List<ParserError> errors,
        CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(e => e.TaxId == row.TaxId, ct);
        if (employee is null)
        {
            errors.Add(new ParserError(row.RowIndex, "TaxId", $"Працівника з ІПН {row.TaxId} не знайдено"));
            return (null, false);
        }
        if (row.WorkedDays > workDaysNorm)
        {
            errors.Add(new ParserError(row.RowIndex, "WorkedDays",
                $"Відпрацьовано днів ({row.WorkedDays}) більше норми місяця ({workDaysNorm})"));
            return (null, false);
        }
        var timesheet = await db.Timesheets.FirstOrDefaultAsync(
            t => t.EmployeeId == employee.Id && t.Year == year && t.Month == month, ct);
        var wasCreated = timesheet is null;
        if (timesheet is null)
        {
            timesheet = new Timesheet
            {
                EmployeeId = employee.Id,
                Year = year,
                Month = month,
            };
            db.Timesheets.Add(timesheet);
        }
        // Пишемо лише 3 поля вводу. Гроші-one-offs (Advance/AnnualBonus/...) лишаємо як були —
        // їх у шаблон не виводимо, тож імпорт їх не чіпає (інакше bulk обнулив би ручні правки з CRUD).
        timesheet.WorkedDays = Round(row.WorkedDays);
        timesheet.ReplacementHours = Round(row.ReplacementHours);
        timesheet.NightHours = Round(row.NightHours);
        return (timesheet, wasCreated);
    }
    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
