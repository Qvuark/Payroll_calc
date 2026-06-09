using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Вислуга медпрацівника — 30% від обчисленого окладу (відомість Y = J×30%).
/// Прапорець HasMedicTenure; false → немає.
/// </summary>
public static class MedicTenureCalculator
{
    private const decimal Pct = 0.30m;

    /// <param name="oklad">Сума окладу цієї ставки (результат OkladCalculator).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal oklad)
    {
        if (!pos.HasMedicTenure)
            return null;

        var amount = oklad * Pct;
        return new CalcComponent("Вислуга медпрацівника", amount, $"={Num(oklad)}*{Num(Pct * 100)}%");
    }
}
