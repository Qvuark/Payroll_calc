namespace PayrollCalc.Documents.Export;

/// <summary>
/// Спільні текстові дрібниці для Excel-вигрузок (назви місяців тощо).
/// </summary>
internal static class ExportText
{
    private static readonly string[] MonthsUk =
        ["січень", "лютий", "березень", "квітень", "травень", "червень",
         "липень", "серпень", "вересень", "жовтень", "листопад", "грудень"];

    /// <summary>
    /// Назва місяця українською в називному відмінку (1 → "січень").
    /// </summary>
    public static string MonthUk(int month) => MonthsUk[month - 1];
}
