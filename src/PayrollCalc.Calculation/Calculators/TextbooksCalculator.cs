using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за підручники — 8% від обчисленого окладу (відомість X = J×8%).
/// Прапорець HasTextbooks; false → немає.
/// </summary>
public static class TextbooksCalculator
{
    private const decimal Pct = 0.08m;

    /// <param name="oklad">Сума окладу цієї ставки (результат OkladCalculator).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal oklad)
    {
        if (!pos.HasTextbooks)
            return null;

        var amount = oklad * Pct;
        return new CalcComponent("За підручники", amount, $"={Num(oklad)}*{Num(Pct * 100)}%");
    }
}
