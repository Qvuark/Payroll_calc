using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за складність і напруженість праці — відсоток від окладу (відомість BA, зазвичай 50% у дир/бухгалтерів).
/// Відсоток зі ставки (ComplexityPct); 0 → доплати немає. База — голий оклад (без №1749).
/// </summary>
public static class ComplexityCalculator
{
    public static CalcComponent? Calc(PositionCalcInput pos)
    {
        if (pos.ComplexityPct == 0)
            return null;

        var amount = pos.Oklad * pos.ComplexityPct;
        var formula = $"={Num(pos.Oklad)}*{Num(pos.ComplexityPct * 100)}%";
        return new CalcComponent("Складність і напруженість", amount, formula);
    }
}
