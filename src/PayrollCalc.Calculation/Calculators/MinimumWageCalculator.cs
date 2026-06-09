using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата до мінімальної зарплати (відомість AK) — витягує низькооплачуваних до МЗП.
/// Поріг рахується по кожній ставці окремо: денна = МЗП×ставки[×дні/норма], погодинна = МЗП/176×години.
/// Доплата = сума порогів − уже нараховане, що зараховується. ≥ порога → доплати немає (null).
/// </summary>
public static class MinimumWageCalculator
{
    private const int HourlyMonthNorm = 176;

    /// <param name="mzp">Мінімальна зарплата на повну ставку (SystemParams, 8647).</param>
    /// <param name="positions">Ставки працівника — кожна дає свій поріг (денний чи погодинний).</param>
    /// <param name="countedEarnings">Сума нарахувань, що зараховуються в мінімалку.</param>
    public static CalcComponent? Calc(
        decimal mzp,
        IReadOnlyList<PositionCalcInput> positions,
        decimal countedEarnings,
        int normDays,
        decimal workedDays)
    {
        decimal threshold = 0m;
        var parts = new List<string>();
        foreach (var pos in positions)
        {
            if (pos.IsHourly)
            {
                threshold += mzp / HourlyMonthNorm * pos.WorkedHours;
                parts.Add($"{Num(mzp)}/{HourlyMonthNorm}*{Num(pos.WorkedHours)}");
            }
            else
            {
                var part = mzp * pos.RateCount;
                var partFormula = pos.RateCount == 1m ? Num(mzp) : $"{Num(mzp)}*{Num(pos.RateCount)}";
                if (workedDays != normDays)
                {
                    part = part * workedDays / normDays;
                    partFormula += $"/{Num(normDays)}*{Num(workedDays)}";
                }
                threshold += part;
                parts.Add(partFormula);
            }
        }

        var topUp = threshold - countedEarnings;
        if (topUp <= 0)
            return null;

        var formula = "=" + string.Join("+", parts) + $"-{Num(countedEarnings)}";
        return new CalcComponent("Доплата до МЗП", topUp, formula);
    }
}
