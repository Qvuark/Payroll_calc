using Microsoft.EntityFrameworkCore;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Timesheet;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Оркестратор імпорту timesheet.xlsx: Stream → Parser → перевірка календаря → upsert кожного рядка → 1 SaveChanges на файл → звіт.
/// Період (year/month) береться з POST-параметра, не з файлу. Один коміт = атомарність.
/// </summary>
public class TimesheetImporter(TimesheetParser parser, TimesheetUpserter upserter, AppDbContext db)
{
    /// <summary>
    /// Імпортує timesheet.xlsx на (year, month). Повертає звіт: створено/оновлено/пропущено + помилки.
    /// Нема робочого календаря на період → одна помилка на весь файл, 0 рядків оброблено.
    /// </summary>
    /// <param name="xlsx">Потік файлу.</param>
    /// <param name="year">Рік періоду (POST-параметр).</param>
    /// <param name="month">Місяць періоду (POST-параметр).</param>
    /// <param name="ct">Cancellation з боку клієнта.</param>
    public async Task<ImportReport> ImportAsync(Stream xlsx, int year, int month, CancellationToken ct = default)
    {
        var (rows, parseErrors) = parser.Parse(xlsx);
        // Без норми місяця нема з чим звіряти WorkedDays. Нема календаря → стоп усього файлу.
        var calendar = await db.WorkCalendars.FirstOrDefaultAsync(wc => wc.Year == year && wc.Month == month, ct);
        if (calendar is null)
            return new ImportReport(0, 0, 0, parseErrors
                .Append(new ParserError(0, null, $"Немає робочого календаря за {month:00}.{year}"))
                .ToList());
        var importErrors = new List<ParserError>();
        var created = 0;
        var updated = 0;
        var skipped = 0;
        // Дедуп у межах файлу: шаблон має унікальні ІПН, але ручна правка може продублювати рядок.
        // Дубль порушив би unique-індекс (EmployeeId,Year,Month) і відкотив би весь файл → краще пропустити з попередженням.
        var seenTaxIds = new HashSet<string>();
        foreach (var row in rows)
        {
            if (row.TaxId is not null && !seenTaxIds.Add(row.TaxId))
            {
                importErrors.Add(new ParserError(row.RowIndex, "TaxId",
                    $"Дубль ІПН {row.TaxId} у файлі — рядок пропущено", ErrorSeverity.Warning));
                skipped++;
                continue;
            }
            var (entity, wasCreated) = await upserter.UpsertAsync(row, year, month, calendar.WorkDays, importErrors, ct);
            if (wasCreated)
                created++;
            else if (entity is not null)
                updated++;
            else
                skipped++;
        }
        // 1 коміт на весь файл = атомарність: збій → відкат усіх змін.
        await db.SaveChangesAsync(ct);
        return new ImportReport(created, updated, skipped, parseErrors.Concat(importErrors).ToList());
    }
}
