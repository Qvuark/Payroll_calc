using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Оплата замін уроків (відомість AW) = ставка_за_годину × години замін.
/// Ставку рахуємо самі (формула бухгалтера): (оклад+№1749+звання)×(1+вислуга+престиж) / 76.2,
/// де 76.2 — середня місячна норма годин. Напр. Скирда: 19523.03/76.2 = 256.21 за годину.
/// 0 годин замін → оплати немає.
/// </summary>
public static class ReplacementCalculator
{
    private const decimal MonthHourNorm = 76.2m;

    /// <param name="bonus1749Rate">Ставка №1749 (0.40) для бази оклад+1749+звання.</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal bonus1749Rate)
    {
        if (pos.ReplacementHours == 0)
            return null;

        // База години заміни = (оклад+1749+звання) з вислугою та престижністю, поділена на норму годин.
        var raisedBase = pos.Oklad * (1 + bonus1749Rate + pos.TitlePct);
        var withTenurePrestige = raisedBase * (1 + pos.TenurePct + pos.PrestigePct);
        var amount = withTenurePrestige / MonthHourNorm * pos.ReplacementHours;
        var formula = $"={Num(withTenurePrestige)}/{Num(MonthHourNorm)}*{Num(pos.ReplacementHours)}";
        return new CalcComponent("Заміни", amount, formula);
    }
}
