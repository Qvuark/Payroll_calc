using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Import.Staff;

/// <summary>
/// Розкладка staff.xlsx: де лежать заголовки, опис українською для бухгалтера,
/// індекси кожної колонки. Один map шарять Parser, HeaderValidator і TemplateGenerator.
/// </summary>
public class StaffColumnMap : IExcelColumnMap
{
    public int HeaderRowIndex => 0;
    public int DescriptionRowIndex => 1;
    public int FirstDataRowIndex => 2;
    public IReadOnlyDictionary<int, string> ExpectedHeaders => Headers;
    public IReadOnlyDictionary<int, string> Descriptions => UkrDescriptions;
    // Іменовані індекси — парсер пише row[ColTabNumber] замість row[0].
    // Якщо колонки переїдуть, міняємо одну константу, а не 28 magic numbers.
    public const int ColTabNumber = 0;
    public const int ColFullName = 1;
    public const int ColTaxId = 2;
    public const int ColHireDate = 3;
    public const int ColEducation = 4;
    public const int ColTitleType = 5;
    public const int ColIsHonored = 6;
    public const int ColHonoredAmount = 7;
    public const int ColPedExpYears = 8;
    public const int ColGeneralExpYears = 9;
    public const int ColSocialBenefitPct = 10;
    public const int ColComplexityPct = 11;
    public const int ColPosition = 12;
    public const int ColPositionStartDate = 13;
    public const int ColTariffGrade = 14;
    public const int ColStavki = 15;
    public const int ColIsPrimary = 16;
    public const int ColHasMilitary = 17;
    public const int ColHasUnfavorable = 18;
    public const int ColGpdGrade = 19;
    public const int ColGpdRate = 20;
    public const int ColPkrGrade = 21;
    public const int ColPkrHours = 22;
    public const int ColMentorAmount = 23;
    public const int ColLibraryMgmtAmount = 24;
    public const int ColTextbooksAmount = 25;
    public const int ColDisinfectants = 26;
    public const int ColNightShifts = 27;
    // Технічні заголовки row 0 — англійською, парсер по них валідує структуру файлу.
    private static readonly Dictionary<int, string> Headers = new()
    {
        { 0, "TabNumber" },
        { 1, "FullName" },
        { 2, "TaxId" },
        { 3, "HireDate" },
        { 4, "Education" },
        { 5, "TitleType" },
        { 6, "IsHonored" },
        { 7, "HonoredAmount" },
        { 8, "PedExpYears" },
        { 9, "GeneralExpYears" },
        { 10, "SocialBenefitPct" },
        { 11, "ComplexityPct" },
        { 12, "Position" },
        { 13, "PositionStartDate" },
        { 14, "TariffGrade" },
        { 15, "Stavki" },
        { 16, "IsPrimary" },
        { 17, "HasMilitary" },
        { 18, "HasUnfavorable" },
        { 19, "GpdGrade" },
        { 20, "GpdRate" },
        { 21, "PkrGrade" },
        { 22, "PkrHours" },
        { 23, "MentorAmount" },
        { 24, "LibraryMgmtAmount" },
        { 25, "TextbooksAmount" },
        { 26, "Disinfectants" },
        { 27, "NightShifts" },
    };
    // Описи row 1 українською — бухгалтер бачить що куди вписувати.
    // TemplateGenerator пише їх у шаблон під заголовками.
    private static readonly Dictionary<int, string> UkrDescriptions = new()
    {
        { 0, "Табельний номер" },
        { 1, "ПІБ" },
        { 2, "ІПН (10 цифр)" },
        { 3, "Дата прийняття" },
        { 4, "Освіта" },
        { 5, "Звання (тільки Class 2)" },
        { 6, "Заслужений працівник освіти (+ якщо так)" },
        { 7, "Сума надбавки 'Заслужений' (грн)" },
        { 8, "Пед.стаж (років)" },
        { 9, "Загальний стаж (років)" },
        { 10, "Соц.пільга % (50/100/150)" },
        { 11, "Складність % (5-50)" },
        { 12, "Посада з довідника" },
        { 13, "Дата прийняття на посаду (якщо ≠ HireDate)" },
        { 14, "Тарифний розряд (діапазон по посаді)" },
        { 15, "К-сть ставок (0.5/1.0/1.5)" },
        { 16, "Головна ставка (+ якщо так)" },
        { 17, "Військовий облік (+ якщо так)" },
        { 18, "Шкідливі умови (+ якщо так)" },
        { 19, "Розряд ГПД (10-14)" },
        { 20, "Ставок ГПД (0.5 чи 1, НЕ години)" },
        { 21, "Розряд ПКР (10-12)" },
        { 22, "Години ПКР на тиждень" },
        { 23, "Сума за наставництво (грн)" },
        { 24, "Сума за бібліотеку (грн)" },
        { 25, "Сума за підручники (грн)" },
        { 26, "Дезінфектант (+ якщо так, +10%)" },
        { 27, "Нічні зміни (+ якщо так, +40%)" },
    };
}
