using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за роботу в інклюзивних класах (відомість U, N-гілка вчителя) =
/// (тариф+1749+звання)×20%/18 × інклюзивні_години, з пропорцією за неповний місяць.
/// 0 годин → доплати немає. (Аномальний flat-варіант адмінів (оклад+1749)×20% — окремо, на diff-фазі.)
/// </summary>
public static class InclusiveCalculator
{
    private const int TeacherHourNorm = 18;
    private const decimal Pct = 0.20m;

    /// <param name="bonus1749Rate">Ставка №1749 (0.40) для бази тариф+1749+звання.</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal bonus1749Rate, int normDays, decimal workedDays)
    {
        if (pos.InclusiveHours == 0)
            return null;

        var raised = pos.Oklad * (1 + bonus1749Rate + pos.TitlePct);
        var amount = raised * Pct / TeacherHourNorm * pos.InclusiveHours;
        var formula = $"={Num(raised)}*{Num(Pct * 100)}%/{TeacherHourNorm}*{Num(pos.InclusiveHours)}";

        (amount, formula) = Prorate(amount, formula, normDays, workedDays);
        return new CalcComponent("Інклюзивні класи", amount, formula);
    }
}
