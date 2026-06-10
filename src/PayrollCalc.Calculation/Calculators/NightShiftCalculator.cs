using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за нічні години (відомість AO, сторож) = тариф/176 × нічні_години × 40%.
/// 176 — місячна норма годин (константа). Ставка 40% із SystemParams. 0 годин → немає.
/// </summary>
public static class NightShiftCalculator
{
    /// <summary>
    /// Місячна норма годин — дільник погодинної ставки сторожа (з еталона `3782/176`).
    /// </summary>
    private const int MonthlyHourNorm = 176;

    /// <param name="rate">Ставка нічних із SystemParams (0.40).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal rate)
    {
        if (pos.NightHours == 0)
            return null;

        var amount = pos.Oklad / MonthlyHourNorm * pos.NightHours * rate;
        var formula = $"={Num(pos.Oklad)}/{MonthlyHourNorm}*{Num(pos.NightHours)}*{Num(rate * 100)}%";
        return new CalcComponent(ComponentNames.NightShift, amount, formula);
    }
}
