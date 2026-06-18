using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за роботу з дезінфікуючими засобами — 10% від окладу × кількість ставок (відомість AN).
/// База залежить від ставок (підтв. бухгалтером): повна — оклад×10%, Шкута 0.5 — оклад/2×10%, Мірошніченко 0.25 — оклад/4×10%.
/// Прапорець зі ставки (HasDisinfectants); false → доплати немає.
/// </summary>
public static class DisinfectantsCalculator
{
    /// <param name="rate">Ставка доплати з SystemParams (0.10).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal rate)
    {
        if (!pos.HasDisinfectants)
            return null;

        var baseOklad = pos.Oklad * pos.RateCount;
        var amount = baseOklad * rate;
        var formula = pos.RateCount == 1
            ? $"={Num(pos.Oklad)}*{Num(rate * 100)}%"
            : $"={Num(pos.Oklad)}*{Num(pos.RateCount)}*{Num(rate * 100)}%";
        return new CalcComponent(ComponentNames.Disinfectants, amount, formula);
    }
}
