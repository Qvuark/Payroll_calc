using ClosedXML.Excel;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Export;

/// <summary>
/// Генерує порожній .xlsx-шаблон зі схеми IExcelColumnMap.
/// Row HeaderRowIndex — англ. ключі (по них парсер валідує файл при імпорті).
/// Row DescriptionRowIndex — укр. підписи (мама бачить що куди вписувати).
/// Row FirstDataRowIndex+ — порожньо, заповнює мама.
/// Один генератор на всі парсери (Teachers, Staff, GPD, ...) — нова мапа = новий шаблон без зайвого коду.
/// </summary>
public class TemplateGenerator
{
    /// <summary>
    /// Створює xlsx у пам'яті: bold header + кольоровий description-рядок + autosize колонок.
    /// Повертає байти для HTTP response (Controller віддасть як file download).
    /// </summary>
    /// <param name="map">Схема колонок конкретного шаблону (Teachers/Staff/...).</param>
    /// <param name="nameOfSheet">Назва листа в xlsx. Дефолт "Sheet1".</param>
    public byte[] Generate(IExcelColumnMap map, string nameOfSheet = "Sheet1")
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(nameOfSheet);
        foreach (var (col, header) in map.ExpectedHeaders)
        {
            var cell = ws.Cell(map.HeaderRowIndex + 1, col + 1);
            cell.Value = header;
            cell.Style.Font.Bold = true;
        }
        foreach (var (col, desc) in map.Descriptions)
        {
            var cell = ws.Cell(map.DescriptionRowIndex + 1, col + 1);
            cell.Value = desc;
            cell.Style.Fill.SetBackgroundColor(XLColor.LightBlue);
        }
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
