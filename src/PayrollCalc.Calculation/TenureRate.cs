namespace PayrollCalc.Calculation;

/// <summary>
/// Відсоток надбавки за вислугу років за стажем (фіксований за порогами, не редагується руками).
/// Пороги: 5+ років → 10%, 10+ → 20%, 20+ → 30%; до 5 років — без надбавки.
/// Білдер викликає це, щоб заповнити PositionCalcInput.TenurePct зі стажу працівника.
/// </summary>
public static class TenureRate
{
    /// <param name="years">Повних років стажу.</param>
    /// <returns>Частка надбавки (0 / 0.10 / 0.20 / 0.30).</returns>
    public static decimal ForYears(int years) => years switch
    {
        >= 20 => 0.30m,
        >= 10 => 0.20m,
        >= 5 => 0.10m,
        _ => 0m,
    };
}
