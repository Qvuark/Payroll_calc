using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Import.Timesheet;

/// <summary>
/// Розкладка timesheet.xlsx. Три групи колонок:
/// 0-4 — pre-fill ростера (№/ІПН/таб/ПІБ/посада), 5-7 — ввід завуча (відпрацьовано/заміна/нічні),
/// 8-14 — сіра довідка навантаження з БД (парсер не читає, лише для очей завуча).
/// Один map шарять TemplateGenerator (малює всі колонки) і TimesheetParser (читає ІПН + 3 числа).
/// </summary>
public class TimesheetColumnMap : IExcelColumnMap
{
    public int HeaderRowIndex => 0;
    public int DescriptionRowIndex => 1;
    public int FirstDataRowIndex => 2;
    public IReadOnlyDictionary<int, string> ExpectedHeaders => Headers;
    public IReadOnlyDictionary<int, string> Descriptions => UkrDescriptions;
    // Іменовані індекси — код пише row[ColTaxId] замість row[1].
    // Колонка переїхала — міняємо одну константу, а не magic numbers по всьому коду.
    // 0-4 — pre-fill ростера: генератор ставить з БД, завуч не чіпає.
    public const int ColRowNo = 0;
    public const int ColTaxId = 1;
    public const int ColTabNumber = 2;
    public const int ColFullName = 3;
    public const int ColPosition = 4;
    // 5-7 — ввід завуча. Єдине (разом з ІПН), що парсер реально зчитує з файлу.
    public const int ColWorkedDays = 5;
    public const int ColReplacementHours = 6;
    public const int ColNightHours = 7;
    // 8-14 — сіра довідка навантаження з БД (години по класах + к-сть ставок).
    // Парсер їх НЕ читає: їх нема в Headers, тож HeaderValidator їх не вимагає й не звіряє.
    // Потрібні лише щоб завуч бачила навантаження, як у звичному паперовому табелі.
    public const int ColTariff1To4 = 8;
    public const int ColTariffInd1To4 = 9;
    public const int ColTariff5To9 = 10;
    public const int ColTariffInd5To9 = 11;
    public const int ColTariff10To11 = 12;
    public const int ColTariffInd10To11 = 13;
    public const int ColRateCount = 14;

    // Технічні заголовки row 0 (англ.) — контракт парсера: HeaderValidator звіряє лише ці колонки.
    // Навмисно лише 0-7 (що парсер читає). Сірі 8-14 не додаємо — інакше валідатор вимагав би їх у файлі.
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
    // Описи row 1 українською — завуч бачить що куди. TemplateGenerator малює їх у шаблон.
    // Тут є й сірі 8-14 (на відміну від Headers) — щоб довідкові колонки теж мали підпис.
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
        { 8, "1-4 кл (довідка)" },
        { 9, "1-4 інд (довідка)" },
        { 10, "5-9 кл (довідка)" },
        { 11, "5-9 інд (довідка)" },
        { 12, "10-11 кл (довідка)" },
        { 13, "10-11 інд (довідка)" },
        { 14, "Ставки (довідка)" },
    };
    // Пояснення під таблицею для завуча. Генератор малює їх у колонці A нижче даних,
    // парсер пропускає (нема TaxId). Лише для timesheet — у staff/teachers сірих колонок нема.
    public IReadOnlyList<string> FooterNotes =>
    [
        "Як заповнювати:",
        "• Білі колонки — впишіть: відпрацьовано днів, заміна (год), нічні (год).",
        "• Сірі колонки — довідка з бази (навантаження по класах, ставки), при імпорті не зчитуються.",
        "• Навантаження змінилось? Виправте в картці працівника і перегенеруйте табель.",
    ];
}
