using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата до мінімальної зарплати (відомість AK) — витягує низькооплачуваних до МЗП.
/// Доплата = МЗП×коеф_ставок[×пропорція] − уже нараховане, що зараховується в мінімалку.
/// Якщо нараховане вже ≥ мінімалки → доплати немає (null). Рахується ОСТАННЬОЮ з нарахувань.
/// </summary>
public static class MinimumWageCalculator
{
    /// <param name="mzp">Мінімальна зарплата на повну ставку (SystemParams, 8647).</param>
    /// <param name="rateCoefficient">Сумарна кількість ставок працівника (коеф: 1, 0.5, 1.5).</param>
    /// <param name="countedEarnings">Сума нарахувань, що зараховуються в мінімалку (оклад + основні надбавки).</param>
    public static CalcComponent? Calc(
        decimal mzp,
        decimal rateCoefficient,
        decimal countedEarnings,
        int normDays,
        decimal workedDays)
    {
        var threshold = mzp * rateCoefficient;
        var formula = rateCoefficient == 1m ? $"={Num(mzp)}" : $"={Num(mzp)}*{Num(rateCoefficient)}";
        (threshold, formula) = Prorate(threshold, formula, normDays, workedDays);

        var topUp = threshold - countedEarnings;
        if (topUp <= 0)
            return null;

        formula += $"-{Num(countedEarnings)}";
        return new CalcComponent("Доплата до МЗП", topUp, formula);
    }
}
