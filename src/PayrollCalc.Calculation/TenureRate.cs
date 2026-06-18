namespace PayrollCalc.Calculation;

/// <summary>
/// Відсоток надбавки за вислугу років за стажем (фіксований за порогами, не редагується руками).
/// Пороги (КМУ №78): стаж ≥3 р → 10%, ≥10 → 20%, ≥20 → 30%; до 3 — без надбавки.
/// Білдер викликає це, щоб заповнити PositionCalcInput.TenurePct зі стажу працівника.
/// </summary>
public static class TenureRate
{
    /// <param name="years">Повних років стажу.</param>
    /// <returns>Частка надбавки (0 / 0.10 / 0.20 / 0.30).</returns>
    public static decimal ForYears(int years)
    {
        switch (years)
        {
            case >= 20:
                return 0.30m;
            case >= 10:
                return 0.20m;
            case >= 3:
                return 0.10m;
            default:
                return 0m;
        }
    }
}
