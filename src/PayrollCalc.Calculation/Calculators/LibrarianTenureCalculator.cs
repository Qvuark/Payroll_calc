using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Вислуга бібліотекаря — 30% від обчисленого окладу (відомість V = J×30%).
/// База — оклад цієї ставки (не сирий тариф). Прапорець HasLibrarianTenure; false → немає.
/// </summary>
public static class LibrarianTenureCalculator
{
    private const decimal Pct = 0.30m;

    /// <param name="oklad">Сума окладу цієї ставки (результат OkladCalculator).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal oklad)
    {
        if (!pos.HasLibrarianTenure)
            return null;

        var amount = oklad * Pct;
        return new CalcComponent("Вислуга бібліотекаря", amount, $"={Num(oklad)}*{Num(Pct * 100)}%");
    }
}
