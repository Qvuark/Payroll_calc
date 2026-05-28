using ClosedXML.Excel;
using PayrollCalc.Documents.Import.Staff;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Хелпер для integration-тестів StaffImporter: будує валідний staff.xlsx у пам'яті.
/// На відміну від StaffSheetBuilder (який віддає DataTable для unit-тестів парсера)
/// — тут реальний xlsx-stream, як приходить з браузера у продакшні.
/// </summary>
internal static class StaffXlsxBuilder
{
    private static readonly StaffColumnMap Map = new();

    /// <summary>
    /// Будує xlsx з headers row + data rows (descriptions row пропущений — парсеру не потрібен).
    /// dataRows — масив масивів object?. Кожен внутрішній — одна строка Excel.
    /// </summary>
    public static MemoryStream Build(params object?[][] dataRows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Staff");
        // Row 1 (HeaderRowIndex+1 у 1-based ClosedXML) — англ. ключі.
        foreach (var (col, header) in Map.ExpectedHeaders)
            ws.Cell(Map.HeaderRowIndex + 1, col + 1).Value = header;
        // Row 3+ (FirstDataRowIndex+1) — дані. Row 2 (descriptions) пропущена,
        // парсер дивиться на header rowIndex та first data rowIndex з мапи, descriptions ігнорує.
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
        // Reset позиції на початок — Stream в Importer читає з поточної позиції,
        // після SaveAs курсор стоїть в кінці, без Seek прочитає 0 байт.
        ms.Position = 0;
        return ms;
    }

    /// <summary>
    /// Повертає валідну строку з усіма mandatory полями. Дзеркалить StaffSheetBuilder.ValidRow
    /// але як object?[] зручний для прямої передачі у Build. Модифікуй через індекс перед використанням.
    /// </summary>
    public static object?[] ValidRow()
    {
        var row = new object?[Map.ExpectedHeaders.Count];
        row[StaffColumnMap.ColTabNumber] = "S001";
        row[StaffColumnMap.ColFullName] = "Сидоренко Анна Іванівна";
        row[StaffColumnMap.ColTaxId] = "9876543210";
        row[StaffColumnMap.ColHireDate] = "01.09.2020";
        row[StaffColumnMap.ColPosition] = "Бухгалтер";
        row[StaffColumnMap.ColStavki] = 1.0;
        row[StaffColumnMap.ColTariffGrade] = 12;
        return row;
    }

    private static void SetCell(IXLCell cell, object value)
    {
        // ClosedXML Cell.Value не приймає object — потрібен явний type-switch.
        // Підтримуємо типи які реально кладемо у тестах.
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
