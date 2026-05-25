using System.Data;
using PayrollCalc.Documents.Import.Teachers;

namespace PayrollCalc.Tests.Documents.Import.Teachers;

/// <summary>
/// Хелпер для тестів TeachersParser: будує DataTable з правильною структурою
/// (row 0 = headers, row 1 = descriptions, row 2+ = data).
/// </summary>
internal static class TeachersSheetBuilder
{
    private static readonly TeachersColumnMap Map = new();

    /// <summary>
    /// Будує валідну sheet з headers + descriptions + переданими data rows.
    /// Кожна data row — масив з 40 object?, де null = порожня ячейка.
    /// </summary>
    public static DataTable BuildValid(params object?[][] dataRows)
    {
        var dt = new DataTable();
        // 40 колонок, типізація object — як ExcelDataReader віддає.
        for (int i = 0; i < Map.ExpectedHeaders.Count; i++)
            dt.Columns.Add($"c{i}", typeof(object));
        // Row 0 — headers
        var header = dt.NewRow();
        foreach (var (col, name) in Map.ExpectedHeaders)
            header[col] = name;
        dt.Rows.Add(header);

        // Row 1 — descriptions (фактично нам байдуже, парсер їх ігнорує)
        var desc = dt.NewRow();
        foreach (var (col, text) in Map.Descriptions)
            desc[col] = text;
        dt.Rows.Add(desc);

        // Row 2+ — data
        foreach (var rowData in dataRows)
        {
            var row = dt.NewRow();
            for (int i = 0; i < rowData.Length && i < dt.Columns.Count; i++)
                row[i] = rowData[i] ?? DBNull.Value;
            dt.Rows.Add(row);
        }
        return dt;
    }

    /// <summary>
    /// Будує одну повну валідну строку даних — всі mandatory поля заповнені.
    /// Optional поля null. Використовуй у тестах де треба "одна правильна строка"
    /// і потім модифікуй конкретні поля через індекс.
    /// </summary>
    public static object?[] ValidRow()
    {
        var row = new object?[Map.ExpectedHeaders.Count];
        row[TeachersColumnMap.ColTabNumber] = "T001";
        row[TeachersColumnMap.ColFullName] = "Іваненко Іван Іванович";
        row[TeachersColumnMap.ColTaxId] = "1234567890";
        row[TeachersColumnMap.ColHireDate] = "01.09.2020";
        row[TeachersColumnMap.ColPosition] = "Вчитель";
        row[TeachersColumnMap.ColStavki] = 1.0;
        row[TeachersColumnMap.ColTariffGrade] = 12;
        return row;
    }

    /// <summary>
    /// Будує повністю порожню строку (40 null). Для тестів skip-empty
    /// та multi-row scenarios де треба "проміжний пустий рядок".
    /// </summary>
    public static object?[] EmptyRow() => new object?[Map.ExpectedHeaders.Count];
}
