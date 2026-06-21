using PayrollCalc.Core.DTOs.Calculation;

namespace PayrollCalc.Calculation.AvgSalary;

/// <summary>
/// Лікарняні за КМУ №1266: середньоденна = заробіток за 12 міс / (365 − дні відсутності),
/// помножена на дні хвороби та відсоток страхового стажу. Перші 5 днів платить школа, решта — ФСС.
/// Знаменник календарний (хворіють і у вихідні), святкові з нього НЕ виключаються.
/// </summary>
public static class SickLeaveCalculator
{
    /// <summary>
    /// Відсоток оплати лікарняного за страховим стажем (бухгалтер вводить з трудової):
    /// до 3 років — 50%, 3-5 — 60%, 5-8 — 70%, 8 і більше — 100%.
    /// </summary>
    public static decimal PaymentPct(int seniorityYears)
    {
        switch (seniorityYears)
        {
            case < 3:
                return 0.50m;
            case < 5:
                return 0.60m;
            case < 8:
                return 0.70m;
            default:
                return 1.00m;
        }
    }
    /// <param name="baseAmount">Заробіток за 12 місяців (сума компонентів де include_sick = true).</param>
    /// <param name="excludedDays">Дні минулих відсутностей у тих 12 міс (хвороби, декрет, без збереження зп).</param>
    /// <param name="daysTotal">Скільки днів триває ця хвороба.</param>
    /// <param name="paymentPct">Відсоток за стажем — з PaymentPct або ручний override.</param>
    public static SickLeaveResult Calc(decimal baseAmount, int excludedDays, int daysTotal, decimal paymentPct)
    {
        var baseDays = 365 - excludedDays;
        var averageDaily = baseAmount / baseDays;
        var daysEmployer = Math.Min(daysTotal, 5);
        var daysFss = Math.Max(daysTotal - 5, 0);
        var amountEmployer = averageDaily * daysEmployer * paymentPct;
        var amountFss = averageDaily * daysFss * paymentPct;
        return new SickLeaveResult(averageDaily, daysEmployer, daysFss, amountEmployer, amountFss, amountEmployer + amountFss);
    }
}
