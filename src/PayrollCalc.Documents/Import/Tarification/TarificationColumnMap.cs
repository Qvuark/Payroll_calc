namespace PayrollCalc.Documents.Import.Tarification;

/// <summary>
/// Мапа колонок Excel-файлу тарифікації (97 фізичних cols, парні рядки).
/// Ізолює magic numbers (індекси колонок) від коду парсера.
/// </summary>
public static class TarificationColumnMap
{
    public const int HeaderRowIndex = 1;
    public const int FirstDataRowIndex = 4;

    /// <summary>
    /// Очікувані заголовки для перевірки що файл — наша тарифікація.
    /// Якщо хоча б один не співпадає — імпорт зупиняється.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, string> ExpectedHeaders = new Dictionary<int, string>
    {
        { 1, "таб номер" },
        { 2, "П. І. Б." },
        { 8, "Тарифний розряд" },
        { 9, "Ставка на місяць" },
        { 86, "Адмін оклад" },
        { 93, "Основна зарплата" },
    };
    // ╔══════════════════════════════════════════════════════════════╗
    // ║ SOURCE — вхідні дані які треба парсити в DTO                 ║
    // ╚══════════════════════════════════════════════════════════════╝
    // === Base + reference (cols 1-9) ===
    public const int ColTabNumber = 1;
    public const int ColFullName = 2;
    public const int ColPosition = 3;
    public const int ColTitle = 4;
    public const int ColEducation = 5;
    public const int ColCategory = 6;
    public const int ColTeachingExperience = 7;
    public const int ColTariffGrade = 8;
    public const int ColMonthlyRate = 9;

    // === Allowances % (cols 10, 11, 13, 15, 16) ===
    public const int ColTenurePct = 10;
    public const int ColBonus1749 = 11;
    public const int ColTitlePct = 13;
    public const int ColNotebookPct = 15;
    public const int ColInclusivePct = 16;

    // === Workload — навантаження (cols 18-20, 22-24, 26-27) ===
    public const int ColHours1_4 = 18;
    public const int ColIndividualHours1_4 = 19;
    public const int ColInclusiveHours1_4 = 20;
    public const int ColHours5_9 = 22;
    public const int ColIndividualHours5_9 = 23;
    public const int ColInclusiveHours5_9 = 24;
    public const int ColHours10_11 = 26;
    public const int ColIndividualHours10_11 = 27;

    // === Admin block — позитивні надбавки % (cols 51, 53, 54, 56, 58, 60, 62, 64) ===
    public const int ColClassLeaderPct = 51;
    public const int ColOfficeMaintenancePct = 53;
    public const int ColOfficeName = 54;
    public const int ColGymPct = 56;
    public const int ColShootingRangePct = 58;
    public const int ColComputerPct = 60;
    public const int ColExtracurricularPct = 62;
    public const int ColWebsitePct = 64;

    // === Inclusive — години (col 67) ===
    public const int ColInclusiveHoursTotal = 67;

    // === Gpd — група подовженого дня (cols 72-74) ===
    public const int ColGpdGrade = 72;
    public const int ColGpdHours = 73;
    public const int ColGpdBonus1749 = 74;

    // === Pkr — підготовка кадрового резерву (cols 77, 78, 80) ===
    public const int ColPkrGrade = 77;
    public const int ColPkrHours = 78;
    public const int ColPkrBonus1749 = 80;

    // === NonPedagogical (cols 83-85) ===
    public const int ColLibraryHead = 83;
    public const int ColTextbookAmount = 84;
    public const int ColMentorAmount = 85;

    // === Admin oklad — Class 2 (cols 86-87) ===
    public const int ColAdminRate = 86;
    public const int ColAdminBonus1749 = 87;

    // ╔══════════════════════════════════════════════════════════════╗
    // ║ CONTROL — суми пораховані бухгалтером, для звірки формул     ║
    // ╚══════════════════════════════════════════════════════════════╝

    // === Control: Allowances 100% sums (cols 12, 14, 17) ===
    public const int ColControlBonus1749Sum = 12;
    public const int ColControlTitle100 = 14;
    public const int ColControlInclusive100 = 17;

    // === Control: Notebooks sums (cols 21, 25, 28) ===
    public const int ColControlNotebooks1_4Sum = 21;
    public const int ColControlNotebooks5_9Sum = 25;
    public const int ColControlNotebooks10_11Sum = 28;

    // === Control: класи 1-4 розрахунок (cols 29-32) ===
    public const int ColControlSum1_4 = 29;
    public const int ColControlBonus1749In1_4 = 30;
    public const int ColControlTitleIn1_4 = 31;
    public const int ColControlIndividualIn1_4 = 32;

    // === Control: класи 5-9 розрахунок (cols 33-36) ===
    public const int ColControlSum5_9 = 33;
    public const int ColControlBonus1749In5_9 = 34;
    public const int ColControlTitleIn5_9 = 35;
    public const int ColControlIndividualIn5_9 = 36;
    // col 37 — у файлі порожній заголовок, skip

    // === Control: класи 10-11 розрахунок (cols 38-41) ===
    public const int ColControlSum10_11 = 38;
    public const int ColControlBonus1749In10_11 = 39;
    public const int ColControlTitleIn10_11 = 40;
    public const int ColControlIndividualIn10_11 = 41;
    // col 42 — у файлі порожній заголовок, skip

    // === Control: зошити в грошах (cols 43-45) ===
    // Cols 43 і 44 у файлі мають дубльований заголовок "За зошити 5-9 класи" — підозра на баг файлу
    public const int ColControlNotebookMoney5_9_A = 43;
    public const int ColControlNotebookMoney5_9_B = 44;
    public const int ColControlNotebookMoney10_11 = 45;

    // === Control: загальні похідні (cols 46-50) ===
    public const int ColControlPedRate = 46;
    public const int ColControlPrestige = 47;
    public const int ColControlTenure10 = 48;
    public const int ColControlTenure20 = 49;
    public const int ColControlTenure30 = 50;

    // === Control: Admin block суми (cols 52, 55, 57, 59, 61, 63, 65) ===
    public const int ColControlClassLeaderSum = 52;
    public const int ColControlOfficeMaintenanceSum = 55;
    public const int ColControlGymSum = 57;
    public const int ColControlShootingRangeSum = 59;
    public const int ColControlComputerSum = 61;
    public const int ColControlExtracurricularSum = 63;
    public const int ColControlWebsiteSum = 65;

    // === Control: Inclusive sub-block (cols 66, 68-71) ===
    public const int ColControlInclusiveRate = 66;
    public const int ColControlInclusiveBonus1749 = 68;
    public const int ColControlInclusiveTenure = 69;
    public const int ColControlInclusivePrestige = 70;
    public const int ColControlInclusiveExtraPay = 71;

    // === Control: Gpd похідні (cols 75-76) ===
    public const int ColControlGpdTenure = 75;
    public const int ColControlGpdPrestige = 76;

    // === Control: Pkr похідні (cols 79, 81-82) ===
    public const int ColControlPkrSum = 79;
    public const int ColControlPkrPrestige = 81;
    public const int ColControlPkrTenure = 82;

    // === Control: Admin oklad похідні (cols 88-92) ===
    public const int ColControlAdminTitle100 = 88;
    public const int ColControlAdminPrestige = 89;
    public const int ColControlAdminTenure10 = 90;
    public const int ColControlAdminTenure20 = 91;
    public const int ColControlAdminTenure30 = 92;

    // === Control: фінальна перевірка (cols 93, 95) ===
    /// <summary>
    /// Зберігати у ParamsSnapshot.Controls для звірки з нашим розрахунком.
    /// </summary>
    public const int ColMainSalary = 93;

    /// <summary>
    /// Дубль ПІБ для верифікації (col 95). Має співпадати з ColFullName.
    /// </summary>
    public const int ColFullNameVerification = 95;
}
