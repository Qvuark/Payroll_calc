using System.Data;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Import.Timesheet;

/// <summary>
/// Парсер timesheet.xlsx. Stream → (List&lt;TimesheetRowDto&gt;, List&lt;ParserError&gt;).
/// Не throws на bad data — збирає помилки у список, бухгалтер бачить повний звіт за один прохід.
/// Не знає про БД: resolve TaxId→Employee і cross-check — робота Importer/Upserter.
/// </summary>
public class TimesheetParser
{
    private readonly TimesheetColumnMap _map = new();
    /// <summary>
    /// Public entry point — читає Stream, передає DataTable у ParseSheet.
    /// Тонка обгортка над ExcelReaderBase, основна логіка в ParseSheet (тестується ізольовано).
    /// </summary>
    public (List<TimesheetRowDto> rows, List<ParserError> errors) Parse(Stream stream)
    {
        var sheet = ExcelReaderBase.ReadFirstSheet(stream);
        return ParseSheet(sheet);
    }
    /// <summary>
    /// Парсить готовий DataTable. Internal — щоб тести били напряму DataTable без створення
    /// xlsx (швидко, ізольовано). Логіка валідації не залежить від джерела (Stream/xlsx/DataTable).
    /// </summary>
    internal (List<TimesheetRowDto> rows, List<ParserError> errors) ParseSheet(DataTable sheet)
    {
        var dtos = new List<TimesheetRowDto>();
        var errors = new List<ParserError>();
        var headerErrors = HeaderValidator.Validate(
            sheet, _map.HeaderRowIndex, new Dictionary<int, string>(_map.ExpectedHeaders));
        if (headerErrors.Count > 0)
            return (dtos, headerErrors);
        if (sheet.Rows.Count <= _map.FirstDataRowIndex)
        {
            errors.Add(new ParserError(
                _map.FirstDataRowIndex,
                null,
                "В файлі відсутні дані"));
            return (dtos, errors);
        }
        for (int rowNumber = _map.FirstDataRowIndex; rowNumber < sheet.Rows.Count; rowNumber++)
        {
            var row = sheet.Rows[rowNumber];
            // +1 — у Excel нумерація рядків 1-based, у DataTable 0-based.
            // Передаємо "людський" номер, щоб бухгалтер бачив у звіті ту ж цифру, що в Excel.
            var dto = ParseRow(row, rowNumber + 1, errors);
            if (dto is not null)
                dtos.Add(dto);
        }
        return (dtos, errors);
    }
    /// <summary>
    /// Парсить одну строку у TimesheetRowDto. Повертає null якщо рядок порожній (нема TaxId)
    /// або ІПН кривий. Помилки додає у errors, не throws — бухгалтер хоче повний звіт за прохід.
    /// </summary>
    private TimesheetRowDto? ParseRow(DataRow row, int rowNumber, List<ParserError> errors)
    {
        // ─── Skip empty row ───
        // Нема TaxId (pre-filled ключ) — кінець даних або порожній рядок-роздільник. Не помилка.
        var taxRaw = row[TimesheetColumnMap.ColTaxId]?.ToString();
        if (string.IsNullOrWhiteSpace(taxRaw))
            return null;
        // ─── Mandatory + format (10 цифр) ───
        var taxId = ExcelFieldReader.GetMandatoryString(row, TimesheetColumnMap.ColTaxId, "TaxId", rowNumber, errors);
        if (taxId is not null && (taxId.Length != 10 || !taxId.All(char.IsDigit)))
        {
            errors.Add(new ParserError(rowNumber, "TaxId", $"ІПН має складатися з 10 цифр, маємо '{taxId}'"));
            taxId = null;
        }
        if (taxId is null)
            return null;
        // ─── Числа вводу: default 0, кривий формат → error + 0 ───
        var workedDays = ExcelFieldReader.GetOptionalHours(row, TimesheetColumnMap.ColWorkedDays, "WorkedDays", rowNumber, errors);
        var replacementHours = ExcelFieldReader.GetOptionalHours(row, TimesheetColumnMap.ColReplacementHours, "ReplacementHours", rowNumber, errors);
        var nightHours = ExcelFieldReader.GetOptionalHours(row, TimesheetColumnMap.ColNightHours, "NightHours", rowNumber, errors);
        return new TimesheetRowDto
        {
            RowIndex = rowNumber,
            TaxId = taxId,
            WorkedDays = workedDays,
            ReplacementHours = replacementHours,
            NightHours = nightHours,
        };
    }
}
