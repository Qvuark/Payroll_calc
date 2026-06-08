using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за наставництво — 20% від "тариф + №1749" (відомість AD), з пропорцією за неповний місяць.
/// Прапорець зі ставки (IsMentor); false → доплати немає.
/// </summary>
public static class MentorCalculator
{
    private const decimal Pct = 0.20m;

    /// <param name="bonus1749Rate">Ставка №1749 (0.40) — база = тариф×(1+ставка).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal bonus1749Rate, int normDays, decimal workedDays)
    {
        if (!pos.IsMentor)
            return null;

        var raisedOklad = pos.Oklad * (1 + bonus1749Rate);
        var amount = raisedOklad * Pct;
        var formula = $"={Num(raisedOklad)}*{Num(Pct * 100)}%";

        (amount, formula) = Prorate(amount, formula, normDays, workedDays);
        return new CalcComponent("Наставництво", amount, formula);
    }
}
