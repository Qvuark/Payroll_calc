using Microsoft.EntityFrameworkCore;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Timesheet;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Оркестратор імпорту timesheet.xlsx. Пайплайн: Stream → Parser → guard календаря →
/// TimesheetUpserter per row → 1 SaveChanges на весь файл → ImportReport.
/// Період (year/month) приходить з POST-параметра, не з файлу. Атомарність — транзакція EF.
/// </summary>
public class TimesheetImporter(TimesheetParser parser, TimesheetUpserter upserter, AppDbContext db)
{
    /// <summary>
    /// Імпорт timesheet.xlsx на (year, month). Повертає звіт: створено/оновлено/пропущено + помилки
    /// (парсера + резолву). Нема календаря на період → import-level помилка, 0 рядків оброблено.
    /// </summary>
    /// <param name="xlsx">Потік файлу.</param>
    /// <param name="year">Рік періоду (POST-параметр).</param>
    /// <param name="month">Місяць періоду (POST-параметр).</param>
    /// <param name="ct">Cancellation з боку клієнта.</param>
    public async Task<ImportReport> ImportAsync(Stream xlsx, int year, int month, CancellationToken ct = default)
    {
        var (rows, parseErrors) = parser.Parse(xlsx);
        // Guard: без норми місяця не валідуємо WorkedDays. Нема календаря → весь файл стоп.
        var calendar = await db.WorkCalendars.FirstOrDefaultAsync(wc => wc.Year == year && wc.Month == month, ct);
        if (calendar is null)
            return new ImportReport(0, 0, 0, parseErrors
                .Append(new ParserError(0, null, $"Немає робочого календаря за {month:00}.{year}"))
                .ToList());
        var importErrors = new List<ParserError>();
        var created = 0;
        var updated = 0;
        var skipped = 0;
        // Дедуп у межах файлу: pre-filled шаблон має унікальні ІПН, але ручна правка може
        // продублювати рядок. Без цього другий рядок створив би дубль → порушення unique-індексу
        // (EmployeeId,Year,Month) → відкат усього файлу. Краще пропустити з попередженням.
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
        // 1 коміт на весь файл = атомарність. Збій усередині → відкат усіх змін.
        await db.SaveChangesAsync(ct);
        return new ImportReport(created, updated, skipped, parseErrors.Concat(importErrors).ToList());
    }
}
