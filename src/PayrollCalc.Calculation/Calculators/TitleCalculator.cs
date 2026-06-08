using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Надбавка за звання — відсоток від окладу (старший вчитель 10%, методист 15% тощо).
/// Відсоток береться з самої ставки (TitleType.Pct → PositionCalcInput.TitlePct); 0 → надбавки немає (null).
/// База — той самий оклад, що й у №1749 (не компаундиться з ним).
/// </summary>
public static class TitleCalculator
{
    /// <param name="oklad">Сума окладу цієї ставки (результат OkladCalculator).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal oklad)
    {
        if (pos.TitlePct == 0)
            return null;

        var amount = oklad * pos.TitlePct;
        var formula = $"={Num(oklad)}*{Num(pos.TitlePct * 100)}%";
        return new CalcComponent("За звання", amount, formula);
    }
}
