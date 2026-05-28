namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Звіт результату імпорту xlsx-файлу. Повертається з Importer'а у Controller,
/// віддається мамі як JSON: скільки рядків створено/оновлено/пропущено + помилки.
/// Лічильник по рядках Excel (= позиції), не по людях.
/// </summary>
public record ImportReport(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<ParserError> Errors
);
