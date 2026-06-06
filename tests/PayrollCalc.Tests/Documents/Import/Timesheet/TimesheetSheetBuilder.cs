using System.Data;
using System.Linq;
using PayrollCalc.Documents.Import.Timesheet;

namespace PayrollCalc.Tests.Documents.Import.Timesheet;

/// <summary>
/// Хелпер для тестів TimesheetParser: будує DataTable з правильною структурою
/// (row 0 = headers, row 1 = descriptions, row 2+ = data). Дзеркалить StaffSheetBuilder.
/// </summary>
internal static class TimesheetSheetBuilder
{
    private static readonly TimesheetColumnMap Map = new();

    /// <summary>
    /// Будує валідну sheet з headers + descriptions + переданими data rows.
    /// Кожна data row — масив з 8 object?, де null = порожня ячейка.
    /// </summary>
    public static DataTable BuildValid(params object?[][] dataRows)
    {
        var dt = new DataTable();
        // Ширина = найбільша колонка серед Headers і Descriptions (+1). Descriptions має сірі
        // довідкові колонки (8-14), яких нема в Headers — без цього запис опису вилітає за межі.
        var colCount = Map.ExpectedHeaders.Keys.Concat(Map.Descriptions.Keys).Max() + 1;
        for (int i = 0; i < colCount; i++)
            dt.Columns.Add($"c{i}", typeof(object));
        var header = dt.NewRow();
        foreach (var (col, name) in Map.ExpectedHeaders)
            header[col] = name;
        dt.Rows.Add(header);
        var desc = dt.NewRow();
        foreach (var (col, text) in Map.Descriptions)
            desc[col] = text;
        dt.Rows.Add(desc);
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
    /// Валідна строка: TaxId (10 цифр) + 3 числа вводу. Модифікуй через індекс у тестах.
    /// </summary>
    public static object?[] ValidRow()
    {
        var row = new object?[Map.ExpectedHeaders.Count];
        row[TimesheetColumnMap.ColTaxId] = "9876543210";
        row[TimesheetColumnMap.ColWorkedDays] = 20;
        row[TimesheetColumnMap.ColReplacementHours] = 5;
        row[TimesheetColumnMap.ColNightHours] = 8;
        return row;
    }
    /// <summary>
    /// Повністю порожня строка (8 null) — для тестів skip-empty.
    /// </summary>
    public static object?[] EmptyRow() => new object?[Map.ExpectedHeaders.Count];
}
