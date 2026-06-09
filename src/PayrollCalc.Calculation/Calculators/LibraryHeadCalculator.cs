using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Завідування бібліотекою — 50% від обчисленого окладу (відомість W = J×50%).
/// Прапорець IsLibraryHead; false → немає.
/// </summary>
public static class LibraryHeadCalculator
{
    private const decimal Pct = 0.50m;

    /// <param name="oklad">Сума окладу цієї ставки (результат OkladCalculator).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal oklad)
    {
        if (!pos.IsLibraryHead)
            return null;

        var amount = oklad * Pct;
        return new CalcComponent("Завідування бібліотекою", amount, $"={Num(oklad)}*{Num(Pct * 100)}%");
    }
}
