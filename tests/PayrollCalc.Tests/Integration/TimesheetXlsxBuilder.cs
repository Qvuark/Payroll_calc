using ClosedXML.Excel;
using PayrollCalc.Documents.Import.Timesheet;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Хелпер для integration-тестів TimesheetImporter: будує валідний timesheet.xlsx у пам'яті.
/// Реальний xlsx-stream, як приходить з браузера. Дзеркалить StaffXlsxBuilder.
/// </summary>
internal static class TimesheetXlsxBuilder
{
    private static readonly TimesheetColumnMap Map = new();

    /// <summary>
    /// Будує xlsx з header row + data rows (descriptions пропущені — парсеру не потрібні).
    /// </summary>
    public static MemoryStream Build(params object?[][] dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Timesheet");
        foreach (var (col, header) in Map.ExpectedHeaders)
            ws.Cell(Map.HeaderRowIndex + 1, col + 1).Value = header;
        for (int r = 0; r < dataRows.Length; r++)
        {
            var rowData = dataRows[r];
            for (int c = 0; c < rowData.Length; c++)
            {
                var value = rowData[c];
                if (value is null) continue;
                SetCell(ws.Cell(Map.FirstDataRowIndex + 1 + r, c + 1), value);
            }
        }
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        // SaveAs лишає курсор у кінці — без Seek Importer прочитає 0 байт.
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Валідна строка: TaxId + 3 числа. TaxId матчиться з seeded Employee у тесті.
    /// </summary>
    public static object?[] ValidRow()
    {
        var row = new object?[Map.ExpectedHeaders.Count];
        row[TimesheetColumnMap.ColTaxId] = "9876543210";
        row[TimesheetColumnMap.ColWorkedDays] = 20.0;
        row[TimesheetColumnMap.ColReplacementHours] = 5.0;
        row[TimesheetColumnMap.ColNightHours] = 8.0;
        return row;
    }

    private static void SetCell(IXLCell cell, object value)
    {
        switch (value)
        {
            case string s: cell.Value = s; break;
            case int i: cell.Value = i; break;
            case long l: cell.Value = l; break;
            case double d: cell.Value = d; break;
            case decimal dec: cell.Value = dec; break;
            case bool b: cell.Value = b; break;
            case DateTime dt: cell.Value = dt; break;
            default: cell.Value = value.ToString(); break;
        }
    }
}
