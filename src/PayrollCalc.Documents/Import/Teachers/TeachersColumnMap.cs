namespace PayrollCalc.Documents.Import.Teachers;

using PayrollCalc.Documents.Import.Common;

/// <summary>
/// Розкладка teachers.xlsx: де лежать заголовки, опис українською для мами,
/// індекси кожної колонки. Один map шарять Parser, HeaderValidator і TemplateGenerator.
/// </summary>
public class TeachersColumnMap : IExcelColumnMap
{
    public int HeaderRowIndex => 0;
    public int DescriptionRowIndex => 1;
    public int FirstDataRowIndex => 2;
    public IReadOnlyDictionary<int, string> ExpectedHeaders => Headers;
    public IReadOnlyDictionary<int, string> Descriptions => UkrDescriptions;

    // Іменовані індекси — парсер пише row[ColTabNumber] замість row[0].
    // Якщо колонки переїдуть, міняємо одну константу, а не 40 magic numbers.
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
    public const int ColPrestigePct = 12;
    public const int ColPosition = 13;
    public const int ColPositionStartDate = 14;
    public const int ColSubject = 15;
    public const int ColTariffGrade = 16;
    public const int ColStavki = 17;
    public const int ColIsPrimary = 18;
    public const int ColHasMilitary = 19;
    public const int ColHasUnfavorable = 20;
    public const int ColHours1To4 = 21;
    public const int ColIndividualHours1To4 = 22;
    public const int ColHours5To9 = 23;
    public const int ColIndividualHours5To9 = 24;
    public const int ColHours10To11 = 25;
    public const int ColIndividualHours10To11 = 26;
    public const int ColNotebookHours1To4 = 27;
    public const int ColNotebookHours5To9 = 28;
    public const int ColNotebookHours10To11 = 29;
    public const int ColInclusiveHours1To4 = 30;
    public const int ColInclusiveHours5To9 = 31;
    public const int ColInclusiveHours10To11 = 32;
    public const int ColClassMgmt = 33;
    public const int ColCabinetType = 34;
    public const int ColGym = 35;
    public const int ColShooting = 36;
    public const int ColComputers = 37;
    public const int ColExtracurricular = 38;
    public const int ColWebsite = 39;

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
        { 12, "PrestigePct" },
        { 13, "Position" },
        { 14, "PositionStartDate" },
        { 15, "Subject" },
        { 16, "TariffGrade" },
        { 17, "Stavki" },
        { 18, "IsPrimary" },
        { 19, "HasMilitary" },
        { 20, "HasUnfavorable" },
        { 21, "Hours1To4" },
        { 22, "IndividualHours1To4" },
        { 23, "Hours5To9" },
        { 24, "IndividualHours5To9" },
        { 25, "Hours10To11" },
        { 26, "IndividualHours10To11" },
        { 27, "NotebookHours1To4" },
        { 28, "NotebookHours5To9" },
        { 29, "NotebookHours10To11" },
        { 30, "InclusiveHours1To4" },
        { 31, "InclusiveHours5To9" },
        { 32, "InclusiveHours10To11" },
        { 33, "ClassMgmt" },
        { 34, "CabinetType" },
        { 35, "Gym" },
        { 36, "Shooting" },
        { 37, "Computers" },
        { 38, "Extracurricular" },
        { 39, "Website" },
    };

    // Описи row 1 українською — мама бачить що куди вписувати.
    // TemplateGenerator пише їх у шаблон під заголовками.
    private static readonly Dictionary<int, string> UkrDescriptions = new()
    {
        { 0, "Табельний номер" },
        { 1, "ПІБ" },
        { 2, "ІПН (10 цифр)" },
        { 3, "Дата прийняття" },
        { 4, "Освіта" },
        { 5, "Звання (Старший вчитель / Вчитель-методист)" },
        { 6, "Заслужений вчитель (так/ні)" },
        { 7, "Сума надбавки 'Заслужений' (грн)" },
        { 8, "Пед.стаж (років)" },
        { 9, "Загальний стаж (років)" },
        { 10, "Соц.пільга % (50/100/150)" },
        { 11, "Складність % (5-50)" },
        { 12, "Престижність % (5-20, тільки Class 1)" },
        { 13, "Посада (зазвичай 'Вчитель')" },
        { 14, "Дата прийняття на посаду (якщо ≠ HireDate)" },
        { 15, "Предмет (математика / укр.мова / ...)" },
        { 16, "Тарифний розряд (8-18)" },
        { 17, "К-сть ставок (0.5/1.0/1.5)" },
        { 18, "Головна ставка (так/ні)" },
        { 19, "Військовий облік (так/ні)" },
        { 20, "Шкідливі умови (так/ні)" },
        { 21, "Години 1-4 кл" },
        { 22, "Індивідуальні 1-4 кл" },
        { 23, "Години 5-9 кл" },
        { 24, "Індивідуальні 5-9 кл" },
        { 25, "Години 10-11 кл" },
        { 26, "Індивідуальні 10-11 кл" },
        { 27, "Зошити 1-4" },
        { 28, "Зошити 5-9" },
        { 29, "Зошити 10-11" },
        { 30, "Інклюзив 1-4" },
        { 31, "Інклюзив 5-9" },
        { 32, "Інклюзив 10-11" },
        { 33, "Класне керівництво (1-4 / 5-11)" },
        { 34, "Кабінет (звичайний / музика-IT / майстерня)" },
        { 35, "Спортзал (так/ні)" },
        { 36, "Тир (так/ні)" },
        { 37, "Комп'ютерний клас (так/ні)" },
        { 38, "Позакласна (так/ні)" },
        { 39, "Сайт школи (так/ні)" },
    };
}
