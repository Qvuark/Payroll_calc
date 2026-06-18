namespace PayrollCalc.Documents.Import.Timesheet;

/// <summary>
/// Один рядок timesheet.xlsx — типізована копія того, що в Excel.
/// Лише match-ключ (TaxId) + 3 числа для вводу. Решта колонок шаблону (№/таб/ПІБ/посада)
/// pre-filled і парсеру не потрібні. Resolve TaxId→Employee та upsert — робота Importer/Upserter.
/// </summary>
public record TimesheetRowDto
{
    /// <summary>1-based номер рядка у файлі — для error-reporting бухгалтеру.</summary>
    public int RowIndex { get; init; }
    // Природний ключ матчингу. Резолвиться в EmployeeId у TimesheetUpserter.
    public string? TaxId { get; init; }
    // Числа вводу. decimal default 0 — 0 = валідне значення (не "не вказано"), nullable не треба.
    public decimal WorkedDays { get; init; }
    public decimal ReplacementHours { get; init; }
    public decimal NightHours { get; init; }
}
