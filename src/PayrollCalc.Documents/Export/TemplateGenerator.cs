using ClosedXML.Excel;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Export;

/// <summary>
/// Генерує .xlsx-шаблон зі схеми IExcelColumnMap — порожній (staff/teachers) або pre-filled ростером (timesheet).
/// Row HeaderRowIndex — англ. ключі (по них парсер валідує файл при імпорті).
/// Row DescriptionRowIndex — укр. підписи (мама бачить що куди вписувати).
/// Row FirstDataRowIndex+ — порожньо або pre-fill з БД (caller передає рядки), решта колонок заповнює мама.
/// Один генератор на всі парсери (Teachers, Staff, GPD, ...) — нова мапа = новий шаблон без зайвого коду.
/// </summary>
public class TemplateGenerator
{
    /// <summary>
    /// Порожній шаблон (staff/teachers): тільки заголовки + описи, дані вписує мама.
    /// Делегує в pre-fill overload з порожнім ростером.
    /// </summary>
    /// <param name="map">Схема колонок конкретного шаблону (Teachers/Staff/...).</param>
    /// <param name="nameOfSheet">Назва листа в xlsx. Дефолт "Sheet1".</param>
    public byte[] Generate(IExcelColumnMap map, string nameOfSheet = "Sheet1") => Generate(map, [], nameOfSheet);
    /// <summary>
    /// Pre-filled шаблон (timesheet): заголовки + описи + рядки ростера з БД.
    /// Каже мамі що міняти не можна (сірий фон), решта колонок лишаються порожні під ввід.
    /// </summary>
    /// <param name="map">Схема колонок шаблону.</param>
    /// <param name="prefillRows">Рядки ростера: кожен словник = колонка → готове значення з БД. Порожній = шаблон без pre-fill.</param>
    /// <param name="nameOfSheet">Назва листа в xlsx. Дефолт "Sheet1".</param>
    public byte[] Generate(IExcelColumnMap map, IEnumerable<IReadOnlyDictionary<int, string>> prefillRows, string nameOfSheet = "Sheet1")
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
        // Pre-fill: кожен словник = один рядок ростера (колонка → готове значення з БД).
        // Сірий фон сигналить мамі "не чіпати" (ІПН/ПІБ/посада ставить програма). Колонки для
        // вводу у словнику відсутні → лишаються порожні. Autosize нижче — ПІСЛЯ запису даних.
        var dataRow = map.FirstDataRowIndex + 1;
        foreach (var prefillRow in prefillRows)
        {
            foreach (var (col, value) in prefillRow)
            {
                var cell = ws.Cell(dataRow, col + 1);
                cell.Value = value;
                cell.Style.Fill.SetBackgroundColor(XLColor.LightGray);
            }
            dataRow++;
        }
        ws.Columns().AdjustToContents();
        // Легенда-пояснення під даними (як заповнювати). Кожен рядок у колонці A.
        // Пишемо ПІСЛЯ autosize, щоб довгий текст не розтягував колонку. Парсер ці рядки
        // пропускає — у них нема TaxId, спрацьовує skip-empty.
        if (map.FooterNotes.Count > 0)
        {
            dataRow++; // порожній рядок-відступ між даними і легендою
            foreach (var note in map.FooterNotes)
            {
                ws.Cell(dataRow, 1).Value = note;
                dataRow++;
            }
        }
        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }
}
