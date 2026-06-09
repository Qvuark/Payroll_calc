using FluentAssertions;
using PayrollCalc.Calculation;

namespace PayrollCalc.Tests.Calculation;

/// <summary>
/// Пороги вислуги років → %. ⚠️ Значення зі слів мами (2026-06-09, «помоему») — звірити з еталоном.
/// </summary>
public class TenureRateTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 0)]
    [InlineData(5, 0.10)]
    [InlineData(9, 0.10)]
    [InlineData(10, 0.20)]
    [InlineData(19, 0.20)]
    [InlineData(20, 0.30)]
    [InlineData(35, 0.30)]
    public void ForYears_ReturnsRateByThreshold(int years, decimal expected)
        => TenureRate.ForYears(years).Should().Be(expected);
}
