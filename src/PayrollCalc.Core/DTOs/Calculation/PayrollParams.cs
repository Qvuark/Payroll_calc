namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Системні коефіцієнти на місяць (знімок SystemParams). Рушій бере ставки податків і надбавок
/// звідси, а не з констант — щоб зміна закону правилась у БД, і кожен розрахунок ніс свій знімок (аудит).
/// </summary>
public record PayrollParams
{
    /// <summary>
    /// ПДФО, частка від gross (0.18).
    /// </summary>
    public required decimal Pdfo { get; init; }
    /// <summary>
    /// Військовий збір, частка від gross (0.05).
    /// </summary>
    public required decimal Vz { get; init; }
    /// <summary>
    /// Профспілковий внесок, частка (0.01). База = gross − лікарняні ФСС.
    /// </summary>
    public required decimal Union { get; init; }
    /// <summary>
    /// Надбавка №1749 для педагогів, частка окладу (0.40).
    /// </summary>
    public required decimal Bonus1749 { get; init; }
    /// <summary>
    /// Мінімальна зарплата (8647) — стеля для доплати низькооплачуваним до МЗП.
    /// </summary>
    public required decimal Mzp { get; init; }
    /// <summary>
    /// База доплати за роботу в несприятливих умовах (2600).
    /// </summary>
    public required decimal UnfavorableBase { get; init; }
    /// <summary>
    /// Доплата за роботу з дезінфікуючими засобами, частка (0.10).
    /// </summary>
    public required decimal Disinfectants { get; init; }
    /// <summary>
    /// Доплата за нічні години, частка (0.40).
    /// </summary>
    public required decimal NightShifts { get; init; }
    /// <summary>
    /// Прожитковий мінімум для розрахунку пільги
    /// </summary>
    public required decimal SocialBenefitLivingMin { get; init; }
    /// <summary>
    /// Поріг доходу для отримання пільги
    /// </summary>
    public required decimal SocialBenefitCap { get; init; }
}
