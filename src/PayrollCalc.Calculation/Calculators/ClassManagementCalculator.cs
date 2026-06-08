using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за класне керівництво — відсоток від "тариф + №1749" (відомість T = (тариф+1749)×%).
/// Відсоток за групою класів: 1-4 → 20%, 5-11 → 25% (норма; не зі ставки). null-група → не класний керівник.
/// База — голий тариф (Oklad), не погодинний оклад; за неповний місяць діє пропорція /норма×відпрац.
/// </summary>
public static class ClassManagementCalculator
{
    private const decimal PctGrades1To4 = 0.20m;
    private const decimal PctGrades5To11 = 0.25m;

    /// <param name="bonus1749Rate">Ставка №1749 із SystemParams (0.40) — щоб база = тариф×(1+ставка).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal bonus1749Rate, int normDays, decimal workedDays)
    {
        if (pos.ClassManagementGroup is not { } group)
            return null;

        var pct = group == ClassGradeGroup.Grades1To4 ? PctGrades1To4 : PctGrades5To11;
        var raisedOklad = pos.Oklad * (1 + bonus1749Rate);
        var amount = raisedOklad * pct;
        var formula = $"={Num(raisedOklad)}*{Num(pct * 100)}%";

        (amount, formula) = Prorate(amount, formula, normDays, workedDays);
        return new CalcComponent("Класне керівництво", amount, formula);
    }
}
