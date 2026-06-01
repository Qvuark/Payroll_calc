using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Import.Timesheet;

/// <summary>
/// Розкладка timesheet.xlsx: pre-filled ростер (№/ІПН/таб/ПІБ/посада) + 3 колонки для вводу мамою.
/// Один map шарять TemplateGenerator (pre-fill активними) і TimesheetParser (match по ІПН + 3 числа).
/// </summary>
public class TimesheetColumnMap : IExcelColumnMap
{
    public int HeaderRowIndex => 0;
    public int DescriptionRowIndex => 1;
    public int FirstDataRowIndex => 2;
    public IReadOnlyDictionary<int, string> ExpectedHeaders => Headers;
    public IReadOnlyDictionary<int, string> Descriptions => UkrDescriptions;
    // Іменовані індекси — парсер пише row[ColTaxId] замість row[1].
    // Якщо колонки переїдуть, міняємо одну константу, а не magic numbers по всьому парсеру.
    // 0-4 — pre-filled генератором (мама не чіпає), 5-7 — мама вписує числа.
    public const int ColRowNo = 0;
    public const int ColTaxId = 1;
    public const int ColTabNumber = 2;
    public const int ColFullName = 3;
    public const int ColPosition = 4;
    public const int ColWorkedDays = 5;
    public const int ColReplacementHours = 6;
    public const int ColNightHours = 7;
    // Технічні заголовки row 0 — англійською, парсер по них валідує структуру файлу.
    private static readonly Dictionary<int, string> Headers = new()
    {
        { 0, "RowNo" },
        { 1, "TaxId" },
        { 2, "TabNumber" },
        { 3, "FullName" },
        { 4, "Position" },
        { 5, "WorkedDays" },
        { 6, "ReplacementHours" },
        { 7, "NightHours" },
    };
    // Описи row 1 українською — мама бачить що куди вписувати.
    // TemplateGenerator пише їх у шаблон під заголовками.
    private static readonly Dictionary<int, string> UkrDescriptions = new()
    {
        { 0, "№" },
        { 1, "ІПН (10 цифр)" },
        { 2, "Табельний номер" },
        { 3, "ПІБ" },
        { 4, "Посада" },
        { 5, "Відпрацьовано днів" },
        { 6, "Заміна (год)" },
        { 7, "Нічні (год)" },
    };
}
