using PayrollCalc.Core.DTOs.Calculation;

namespace PayrollCalc.Calculation.AvgSalary;

/// <summary>
/// Курси підвищення кваліфікації (КМУ №100): середньоденна = заробіток за 2 місяці до
/// курсів / робочі дні тих 2 місяців, помножена на робочі дні відсутності на курсах.
/// На відміну від лікарняних/відпускних — період лише 2 місяці і знаменник у РОБОЧИХ днях
/// (на курсах працівник був би на роботі лише в робочі дні).
/// </summary>
public static class TrainingCalculator
{
    /// <param name="baseAmount">Заробіток за 2 місяці до курсів (компоненти де include_training = true).</param>
    /// <param name="baseWorkingDays">Робочі дні тих 2 місяців (з work_calendar).</param>
    /// <param name="workingDaysAbsent">Робочі дні відсутності на курсах.</param>
    public static TrainingResult Calc(decimal baseAmount, int baseWorkingDays, int workingDaysAbsent)
    {
        var averageDaily = baseAmount / baseWorkingDays;
        var total = averageDaily * workingDaysAbsent;
        return new TrainingResult(averageDaily, total);
    }
}
