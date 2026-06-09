using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Оплата замін уроків (відомість AW) = ставка_заміни × години замін.
/// Ставка precomputed (вводиться вручну, ≈ (оклад+1749+20%)/76.2); години — з табеля.
/// 0 годин або 0 ставки → оплати немає.
/// </summary>
public static class ReplacementCalculator
{
    public static CalcComponent? Calc(PositionCalcInput pos)
    {
        if (pos.ReplacementRate == 0 || pos.ReplacementHours == 0)
            return null;

        var amount = pos.ReplacementRate * pos.ReplacementHours;
        return new CalcComponent("Заміни", amount, $"={Num(pos.ReplacementRate)}*{Num(pos.ReplacementHours)}");
    }
}
