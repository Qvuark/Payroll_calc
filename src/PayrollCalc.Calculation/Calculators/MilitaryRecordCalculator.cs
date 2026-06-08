using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за ведення військового обліку — 5% від окладу (відомість AJ), з пропорцією за неповний місяць.
/// База — голий оклад (без №1749). Прапорець зі ставки (HasMilitaryRecord); false → доплати немає.
/// </summary>
public static class MilitaryRecordCalculator
{
    private const decimal Pct = 0.05m;

    public static CalcComponent? Calc(PositionCalcInput pos, int normDays, decimal workedDays)
    {
        if (!pos.HasMilitaryRecord)
            return null;

        var amount = pos.Oklad * Pct;
        var formula = $"={Num(pos.Oklad)}*{Num(Pct * 100)}%";

        (amount, formula) = Prorate(amount, formula, normDays, workedDays);
        return new CalcComponent("Військовий облік", amount, formula);
    }
}
