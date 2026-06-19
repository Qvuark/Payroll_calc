using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Шукає звання в довіднику за назвою + класом і повертає його Id.
/// Спільний для Staff і Teachers upserter'ів. Static — стану нема, лише запит до БД.
/// </summary>
public static class TitleTypeResolver
{
    /// <summary>
    /// Шукає TitleType у довіднику по (Name, WorkerClass). Одна назва ("Методист") може існувати
    /// для різних класів з різним %, тому scope обов'язковий.
    /// </summary>
    /// <param name="db">DbContext імпорту (Importer тримає одну транзакцію на файл).</param>
    /// <param name="name">Рядок з xlsx — назва звання. null/whitespace = працівник без звання, не помилка.</param>
    /// <param name="workerClass">Клас посади (береться з Position яку щойно зарезолвив PositionUpserter).</param>
    /// <param name="rowIndex">1-based номер рядка у файлі для error-reporting.</param>
    /// <param name="errors">Колекція помилок звіту — додаємо ParserError якщо звання не в довіднику.</param>
    /// <param name="ct">Cancellation з боку клієнта.</param>
    /// <returns>
    /// Id звання якщо знайдено; null якщо name порожній (без помилки) АБО не знайдено в довіднику (з ParserError у звіт).
    /// Працівник у обох null-кейсах зберігається без TitleTypeId — звання не критичне поле.
    /// </returns>
    public static async Task<int?> ResolveTitleTypeIdAsync(
        AppDbContext db,
        string? name,
        WorkerClass workerClass,
        int rowIndex,
        List<ParserError> errors,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        // Резолв за назвою або синонімом у межах класу. Спершу звужуємо кандидатів по WorkerClass,
        // далі AliasMatcher звіряє Name/ExcelAliases з нормалізацією.
        var candidates = await db.TitleTypes
            .Where(t => t.WorkerClass == workerClass)
            .ToListAsync(ct);
        var matches = AliasMatcher.Match(candidates, name);
        if (matches.Count == 0)
        {
            errors.Add(new ParserError(
                rowIndex,
                "TitleType",
                $"Звання '{name}' не знайдено для класу {workerClass}"));
            return null;
        }
        if (matches.Count > 1)
        {
            errors.Add(new ParserError(
                rowIndex,
                "TitleType",
                $"Звання '{name}' неоднозначне для класу {workerClass} — збіг з кількома записами"));
            return null;
        }
        return matches[0].Id;
    }
}
