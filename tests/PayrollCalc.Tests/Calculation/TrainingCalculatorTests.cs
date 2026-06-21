using FluentAssertions;
using PayrollCalc.Calculation.AvgSalary;

namespace PayrollCalc.Tests.Calculation;

/// <summary>
/// Юніт-тести калькулятора курсів (КМУ №100). Еталону мами нема — перевіряємо формулу
/// та чутливість: знаменник у робочих днях, середня × дні відсутності.
/// </summary>
public class TrainingCalculatorTests
{
    [Fact]
    public void Calc_AverageDaily_IsBaseOverWorkingDays()
    {
        // 70000 / 42 робочих днів = 1666.666...
        var result = TrainingCalculator.Calc(baseAmount: 70000m, baseWorkingDays: 42, workingDaysAbsent: 10);

        Math.Round(result.AverageDaily, 2, MidpointRounding.AwayFromZero).Should().Be(1666.67m);
    }

    [Fact]
    public void Calc_Total_IsAverageTimesDaysAbsent()
    {
        var result = TrainingCalculator.Calc(baseAmount: 70000m, baseWorkingDays: 42, workingDaysAbsent: 10);

        result.Total.Should().Be(result.AverageDaily * 10);
    }

    [Fact]
    public void Calc_FewerWorkingDays_RaisesAverage()
    {
        // Менше робочих днів у знаменнику → вища середньоденна
        var more = TrainingCalculator.Calc(baseAmount: 70000m, baseWorkingDays: 42, workingDaysAbsent: 5);
        var fewer = TrainingCalculator.Calc(baseAmount: 70000m, baseWorkingDays: 40, workingDaysAbsent: 5);

        fewer.AverageDaily.Should().BeGreaterThan(more.AverageDaily);
    }
}
