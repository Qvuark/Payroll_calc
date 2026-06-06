using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Timesheet;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Знаходить або оновлює табель за (EmployeeId, Year, Month). ІПН резолвить у працівника;
/// не знайдено → помилка в звіт, людину НЕ створює (табель лише для наявних).
/// Пише ЛИШЕ 3 поля вводу (відпрацьовано/заміна/нічні) — гроші-one-offs з картки не чіпає. Не комітить — це робить Importer.
/// </summary>
public class TimesheetUpserter(AppDbContext db)
{
    /// <summary>
    /// Знаходить працівника за ІПН і створює/оновлює його табель на (year, month).
    /// Не знайдено або відпрацьовано > норми → помилка в errors + (null, false) = пропуск.
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
        // Пишемо лише 3 поля вводу. Гроші-one-offs (Advance/AnnualBonus/...) не чіпаємо — їх нема в шаблоні,
        // інакше імпорт обнулив би ручні правки з картки.
        timesheet.WorkedDays = Round(row.WorkedDays);
        timesheet.ReplacementHours = Round(row.ReplacementHours);
        timesheet.NightHours = Round(row.NightHours);
        return (timesheet, wasCreated);
    }
    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
