using System.Data;

namespace PayrollCalc.Documents.Import.Common;
/// <summary>
/// Валідатор заголовків Excel-файлу.
/// </summary>
public static class HeaderValidator
{
    /// <summary>
    /// Перевіряє заголовки Excel-файлу на відповідність очікуваним значенням.
    /// </summary>
    /// <param name="sheet">Таблиця з даними з Excel-файлу.</param>
    /// <param name="headerRowIndex">Індекс рядка, що містить заголовки (0-based).</param>
    /// <param name="expectedHeaders">Словник очікуваних заголовків (індекс колонки → текст заголовка).</param>
    /// <returns>
    /// Список знайдених помилок. Порожній список означає що заголовки 
    /// валідні.
    /// </returns>
    public static List<ParserError> Validate(
        DataTable sheet,
        int headerRowIndex,
        Dictionary<int, string> expectedHeaders
    )
    {
        var errors = new List<ParserError>();
        if(sheet.Rows.Count <= headerRowIndex)
        {
            errors.Add(new ParserError(headerRowIndex+1,null, "Файл не містить рядка з заголовками"));
            return errors;
        }
        var headerRow = sheet.Rows[headerRowIndex];
        foreach(var (col,expectedHeader) in expectedHeaders)
        {
            // перевіряємо чи є колонка в таблиці
            if(col>=headerRow.ItemArray.Length)
            {
                errors.Add(new ParserError(
                    headerRowIndex+1,
                    null,
                    $"Бракує колонки {col+1} (очікується {expectedHeader})"));
                    continue;
            }
            var actual = headerRow[col]?.ToString()?.Trim() ?? "";
            // перевірка заголовків з ігноруванням регістра, пробілів та переносу рядка у клітинці
            // всі порожні значення приводимо до пустої строки
            if(!actual.Equals(expectedHeader,StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new ParserError(
                    headerRowIndex+1,
                    col.ToString(),
                    $"Неочікуваний заголовок: очікується '{expectedHeader}', маємо '{actual}'"));
            }
        }
        return errors;
    }
}