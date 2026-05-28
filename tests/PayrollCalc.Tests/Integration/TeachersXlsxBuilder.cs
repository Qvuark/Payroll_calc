using ClosedXML.Excel;
using PayrollCalc.Documents.Import.Teachers;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Хелпер для integration-тестів TeachersImporter: будує валідний teachers.xlsx у пам'яті.
/// Дзеркалить StaffXlsxBuilder, але під TeachersColumnMap (40 колонок).
/// </summary>
internal static class TeachersXlsxBuilder
{
    private static readonly TeachersColumnMap Map = new();

    /// <summary>
    /// Будує xlsx з headers row + data rows. Descriptions row (row 2) пропускається,
    /// парсер дивиться на HeaderRowIndex і FirstDataRowIndex з мапи.
    /// </summary>
    public static MemoryStream Build(params object?[][] dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Teachers");
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
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Повертає валідну строку з усіма mandatory полями. Дефолти: вчитель математики, 1 ставка, розряд 12.
    /// Модифікуй через індекс перед передачею у Build.
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
            case DateOnly dn: cell.Value = dn.ToDateTime(TimeOnly.MinValue); break;
            default: cell.Value = value.ToString(); break;
        }
    }
}
