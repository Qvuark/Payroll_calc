using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за роботу з дезінфікуючими засобами — 10% від окладу (відомість AN).
/// База — голий оклад (без №1749). Прапорець зі ставки (HasDisinfectants); false → доплати немає.
/// </summary>
public static class DisinfectantsCalculator
{
    /// <param name="rate">Ставка доплати з SystemParams (0.10).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal rate)
    {
        if (!pos.HasDisinfectants)
            return null;

        var amount = pos.Oklad * rate;
        var formula = $"={Num(pos.Oklad)}*{Num(rate * 100)}%";
        return new CalcComponent("Дезінфікуючі засоби", amount, formula);
    }
}
