using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Позаурочна робота (ГПД або ПКР) як надбавка до основної ставки (відомість AE/AF + AG/AH/AI).
/// Власний блок: база → підвищення №22 (×40%) → вислуга → престижність, незалежно від основного окладу.
/// База обчислюється як Tariff/Divisor×Hours — одна формула покриває обидва види.
/// </summary>
public record ExtendedActivityInput
{
    /// <summary>
    /// Вид роботи — визначає назву базового компонента ("За ГПД" / "За ПКР").
    /// </summary>
    public required ExtendedActivityKind Kind { get; init; }
    /// <summary>
    /// Тариф для бази: ПКР — місячний оклад розряду ПКР; ГПД — оклад розряду ГПД.
    /// </summary>
    public required decimal Tariff { get; init; }
    /// <summary>
    /// Дільник бази: ПКР — 18 (тижнева норма годин); ГПД — норма ГПД або частка ставки.
    /// </summary>
    public required decimal Divisor { get; init; }
    /// <summary>
    /// Множник годин: ПКР — тижневі години роботи; ГПД — години/дні (1 коли база = частка окладу).
    /// </summary>
    public decimal Hours { get; init; } = 1m;
    /// <summary>
    /// Вислуга цього блоку: % від (база+№22). Береться зі стажу працівника; 0 → вислуги немає.
    /// </summary>
    public decimal TenurePct { get; init; }
    /// <summary>
    /// Пропорція за неповний місяць по днях табеля. ПКР — true; ГПД — false (дні вже в Hours).
    /// </summary>
    public bool ProrateByDays { get; init; }
}
