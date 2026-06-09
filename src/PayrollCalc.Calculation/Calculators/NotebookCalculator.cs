using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за перевірку зошитів (відомість S) = (тариф+звання+1749)/18 × години × відсоток.
/// База — «оклад з підвищенням» від СИРОГО тарифу; за неповний місяць діє пропорція.
/// 0 годин або 0% → доплати немає.
/// </summary>
public static class NotebookCalculator
{
    private const int TeacherHourNorm = 18;

    /// <param name="bonus1749Rate">Ставка №1749 (0.40) для бази тариф+1749.</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal bonus1749Rate, int normDays, decimal workedDays)
    {
        if (pos.NotebookHours == 0 || pos.NotebookPct == 0)
            return null;

        var raised = pos.Oklad * (1 + pos.TitlePct + bonus1749Rate);
        var amount = raised / TeacherHourNorm * pos.NotebookHours * pos.NotebookPct;
        var formula = $"={Num(raised)}/{TeacherHourNorm}*{Num(pos.NotebookHours)}*{Num(pos.NotebookPct * 100)}%";

        (amount, formula) = Prorate(amount, formula, normDays, workedDays);
        return new CalcComponent("За перевірку зошитів", amount, formula);
    }
}
