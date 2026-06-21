using FluentAssertions;
using PayrollCalc.Calculation.AvgSalary;

namespace PayrollCalc.Tests.Calculation;

/// <summary>
/// Юніт-тести калькулятора лікарняних (КМУ №1266). Головний — середньоденна на еталоні
/// мами (348.13). Решта — пороги страхового стажу та поділ днів школа/ФСС на синтетиці.
/// </summary>
public class SickLeaveCalculatorTests
{
    [Fact]
    public void Calc_MomReference_AverageDailyRoundsTo34813()
    {
        // Еталон мами: 123236.58 / (365 − 11) = 348.13
        var result = SickLeaveCalculator.Calc(baseAmount: 123236.58m, excludedDays: 11, daysTotal: 10, paymentPct: 1.00m);

        Math.Round(result.AverageDaily, 2, MidpointRounding.AwayFromZero).Should().Be(348.13m);
    }

    [Theory]
    [InlineData(2, 0.50)]
    [InlineData(4, 0.60)]
    [InlineData(6, 0.70)]
    [InlineData(10, 1.00)]
    [InlineData(3, 0.60)]   // межа: рівно 3 роки → вже 60%
    [InlineData(8, 1.00)]   // межа: рівно 8 років → 100%
    public void PaymentPct_ByInsuranceSeniority(int years, double expectedPct)
    {
        SickLeaveCalculator.PaymentPct(years).Should().Be((decimal)expectedPct);
    }

    [Fact]
    public void Calc_TenDays_SplitsFiveToEmployerFiveToFss()
    {
        var result = SickLeaveCalculator.Calc(baseAmount: 35400m, excludedDays: 0, daysTotal: 10, paymentPct: 1.00m);

        result.DaysEmployer.Should().Be(5);
        result.DaysFss.Should().Be(5);
        result.Total.Should().Be(result.AmountEmployer + result.AmountFss);
    }

    [Fact]
    public void Calc_ShortSickness_AllToEmployer_NoFss()
    {
        var result = SickLeaveCalculator.Calc(baseAmount: 35400m, excludedDays: 0, daysTotal: 3, paymentPct: 0.50m);

        result.DaysEmployer.Should().Be(3);
        result.DaysFss.Should().Be(0);
        result.AmountFss.Should().Be(0m);
    }
}
