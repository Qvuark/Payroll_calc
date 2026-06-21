using FluentAssertions;
using PayrollCalc.Calculation.AvgSalary;

namespace PayrollCalc.Tests.Calculation;

/// <summary>
/// Юніт-тести калькулятора відпускних (КМУ №100). Головний — середньоденна на еталоні
/// мами (директор 745.42). Та сама формула обслуговує компенсацію при звільненні.
/// </summary>
public class VacationCalculatorTests
{
    [Fact]
    public void Calc_MomReference_AverageDailyRoundsTo74542()
    {
        // Еталон мами: директор, 272077.34 / 365 = 745.42
        var result = VacationCalculator.Calc(baseAmount: 272077.34m, baseDays: 365, calendarDays: 56);

        Math.Round(result.AverageDaily, 2, MidpointRounding.AwayFromZero).Should().Be(745.42m);
    }

    [Fact]
    public void Calc_FullPedagogicalVacation_56Days()
    {
        // Педагог: 56 кал. днів × середньоденна еталону
        var result = VacationCalculator.Calc(baseAmount: 272077.34m, baseDays: 365, calendarDays: 56);

        Math.Round(result.Total, 2, MidpointRounding.AwayFromZero).Should().Be(41743.37m);
    }

    [Fact]
    public void Calc_PartialYear_SmallerDenominatorRaisesAverage()
    {
        // Якщо в місяці були дні без збереження зп → знаменник менший → середня вища
        var full = VacationCalculator.Calc(baseAmount: 200000m, baseDays: 365, calendarDays: 24);
        var partial = VacationCalculator.Calc(baseAmount: 200000m, baseDays: 350, calendarDays: 24);

        partial.AverageDaily.Should().BeGreaterThan(full.AverageDaily);
    }
}
