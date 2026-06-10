using FluentAssertions;
using PayrollCalc.Calculation;

namespace PayrollCalc.Tests.Calculation;

/// <summary>
/// Пороги вислуги років → % за КМУ №78: понад 3 → 10%, понад 10 → 20%, понад 20 → 30%.
/// ⚠️ Звірити з еталоном на реальних даних (чи мама округлює стаж так само).
/// </summary>
public class TenureRateTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0.10)]
    [InlineData(9, 0.10)]
    [InlineData(10, 0.20)]
    [InlineData(19, 0.20)]
    [InlineData(20, 0.30)]
    [InlineData(35, 0.30)]
    public void ForYears_ReturnsRateByThreshold(int years, decimal expected)
        => TenureRate.ForYears(years).Should().Be(expected);
}
