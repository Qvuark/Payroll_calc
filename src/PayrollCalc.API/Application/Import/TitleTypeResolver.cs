using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Резолв TitleType string → Id з урахуванням WorkerClass scope.
/// Спільний хелпер для StaffPositionUpserter і TeachersPositionUpserter — резолв логіка
/// ідентична, виносимо щоб не копіпастити. Static бо без стану: тільки query до довідника.
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
        var titleType = await db.TitleTypes
            .FirstOrDefaultAsync(t => t.Name == name && t.WorkerClass == workerClass, ct);
        if (titleType is null)
        {
            errors.Add(new ParserError(
                rowIndex,
                "TitleType",
                $"Звання '{name}' не знайдено для класу {workerClass}"));
            return null;
        }
        return titleType.Id;
    }
}
