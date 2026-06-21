using PayrollCalc.Core.DTOs.Calculation;

namespace PayrollCalc.Calculation.AvgSalary;

/// <summary>
/// Відпускні та компенсація за невикористану відпустку (КМУ №100): середньоденна =
/// заробіток за 12 міс / сума календарних днів тих місяців, помножена на дні відпустки.
/// Знаменник приходить готовим (сервіс рахує суму днів по місяцях, зазвичай 365).
/// Та сама формула обслуговує і відпустку, і компенсацію при звільненні.
/// </summary>
public static class VacationCalculator
{
    /// <param name="baseAmount">Заробіток за 12 місяців (компоненти де include_vacation = true).</param>
    /// <param name="baseDays">Сума фактичних календарних днів кожного місяця за 12 міс (зазвичай 365, менше якщо були дні без збереження зп).</param>
    /// <param name="calendarDays">Календарні дні цієї відпустки (або невикористані дні — для компенсації).</param>
    public static VacationResult Calc(decimal baseAmount, int baseDays, int calendarDays)
    {
        var averageDaily = baseAmount / baseDays;
        var total = averageDaily * calendarDays;
        return new VacationResult(averageDaily, total);
    }
}
