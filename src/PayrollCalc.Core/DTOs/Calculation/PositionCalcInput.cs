using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Одна ставка/посада працівника для розрахунку. Гілка окладу визначається WorkerClass:
/// Pedagogical → погодинний оклад N (тариф/18×год); решта класів → фіксований оклад J.
/// Працівник із кількома ставками (напр. директор, що викладає) має кілька таких записів.
/// Поля надбавок (класне, кабінет, мед, нічні, зошити) додаються в міру реалізації компонентів.
/// </summary>
public record PositionCalcInput
{
    public required WorkerClass WorkerClass { get; init; }
    public required string PositionName { get; init; }
    /// <summary>
    /// Номер тарифного розряду (1-25) — для шапки розрахункового листа. 0 = не заданий.
    /// </summary>
    public int TariffGrade { get; init; }
    /// <summary>
    /// Назва звання («Старший вчитель») — для шапки розрахункового листа. Null = без звання.
    /// </summary>
    public string? TitleName { get; init; }
    /// <summary>
    /// Місячний оклад розряду на повну ставку (TariffGrade.MonthlyRate) — число, що веде формулу.
    /// Для директорозалежних посад тут оклад директора, а множник у DirectorPct.
    /// </summary>
    public required decimal Oklad { get; init; }
    /// <summary>
    /// Кількість ставок (1 = повна, 0.5 = півставки).
    /// </summary>
    public required decimal RateCount { get; init; }
    /// <summary>
    /// % від окладу директора для залежних посад: заступник 0.95, головбух 0.90.
    /// Оклад тоді = Oklad × DirectorPct (формула "=10410*95%"). null — незалежна посада.
    /// </summary>
    public decimal? DirectorPct { get; init; }
    /// <summary>
    /// Звання: % підвищення окладу (TitleType.Pct). 0 якщо звання немає.
    /// </summary>
    public decimal TitlePct { get; init; }
    /// <summary>
    /// Вислуга: % надбавки за стаж. 0 якщо немає (Class 4 MOP не має вислуги).
    /// </summary>
    public decimal TenurePct { get; init; }
    /// <summary>
    /// Престижність: % надбавки (зазвичай 0.20, дир-гілка 0.25). 0 якщо немає.
    /// </summary>
    public decimal PrestigePct { get; init; }
    /// <summary>
    /// N-гілка: тижневе пед.навантаження, годин (відомість E). Базовий оклад = Oklad/18×це.
    /// </summary>
    public decimal PedHoursWeekly { get; init; }
    /// <summary>
    /// N-гілка: надтарифні години (відомість F), додаються до пед.навантаження.
    /// </summary>
    public decimal AdditionalHours { get; init; }
    /// <summary>
    /// Класне керівництво: група класів для відсотка (1-4 → 20%, 5-11 → 25%). null — не класний керівник.
    /// </summary>
    public ClassGradeGroup? ClassManagementGroup { get; init; }
    /// <summary>
    /// Завідування кабінетом: тип для відсотка (звичайний 13%, муз/IT 10%). null — не завідує.
    /// </summary>
    public CabinetType? Cabinet { get; init; }
    /// <summary>
    /// Обслуговування комп'ютерної техніки — доплата 10% (відомість AA).
    /// </summary>
    public bool HasComputerMaintenance { get; init; }
    /// <summary>
    /// Ведення вебсайту — доплата 10% (відомість AB).
    /// </summary>
    public bool HasWebsite { get; init; }
    /// <summary>
    /// Наставництво — доплата 20% (відомість AD).
    /// </summary>
    public bool IsMentor { get; init; }
    /// <summary>
    /// Ведення військового обліку — доплата 5% (відомість AJ).
    /// </summary>
    public bool HasMilitaryRecord { get; init; }
    /// <summary>
    /// Робота з дезінфікуючими засобами — доплата 10% (відомість AN).
    /// </summary>
    public bool HasDisinfectants { get; init; }
    /// <summary>
    /// Складність і напруженість: % надбавки від окладу (дир/бухгалтери ~50%). 0 якщо немає.
    /// </summary>
    public decimal ComplexityPct { get; init; }
    /// <summary>
    /// Вислуга бібліотекаря — доплата 30% від окладу (відомість V).
    /// </summary>
    public bool HasLibrarianTenure { get; init; }
    /// <summary>
    /// Завідування бібліотекою — доплата 50% від окладу (відомість W).
    /// </summary>
    public bool IsLibraryHead { get; init; }
    /// <summary>
    /// За підручники — доплата 8% від окладу (відомість X).
    /// </summary>
    public bool HasTextbooks { get; init; }
    /// <summary>
    /// Вислуга медпрацівника — доплата 30% від окладу (відомість Y).
    /// </summary>
    public bool HasMedicTenure { get; init; }
    /// <summary>
    /// Нічні години за місяць (відомість AO, сторож). Оплата = тариф/176×години×40%.
    /// </summary>
    public decimal NightHours { get; init; }
    /// <summary>
    /// Погодинна посада (сторож): оклад і мінімалка рахуються від відпрацьованих годин, не днів.
    /// </summary>
    public bool IsHourly { get; init; }
    /// <summary>
    /// Відпрацьовані години за місяць (відомість D) — база окладу погодинної посади.
    /// </summary>
    public decimal WorkedHours { get; init; }
    /// <summary>
    /// Зошити: години перевірки зошитів (відомість S). 0 → доплати немає.
    /// </summary>
    public decimal NotebookHours { get; init; }
    /// <summary>
    /// Зошити: % за перевірку (NotebookRate: 10/15/20% за предметом).
    /// </summary>
    public decimal NotebookPct { get; init; }
    /// <summary>
    /// Доплата 2600 за роботу в несприятливих умовах (відомість AY, педагоги).
    /// </summary>
    public bool HasUnfavorable2600 { get; init; }
    /// <summary>
    /// Заміни: ставка за годину заміни (відомість AW) — вводиться вручну (precomputed).
    /// </summary>
    public decimal ReplacementRate { get; init; }
    /// <summary>
    /// Заміни: кількість годин замін за місяць (з табеля).
    /// </summary>
    public decimal ReplacementHours { get; init; }
    /// <summary>
    /// Інклюзив: години в інклюзивних класах (відомість G). 0 → доплати немає.
    /// </summary>
    public decimal InclusiveHours { get; init; }
    /// <summary>
    /// Позаурочна робота (ГПД/ПКР) з власним блоком надбавок (відомість AE/AF). null — немає.
    /// </summary>
    public ExtendedActivityInput? ExtendedActivity { get; init; }
}
