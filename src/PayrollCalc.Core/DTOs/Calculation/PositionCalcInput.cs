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
}
