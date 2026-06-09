using PayrollCalc.Documents.Import.Common;
using FluentAssertions;

namespace PayrollCalc.Tests.Documents.Import.Common;

public class DecimalParserTests
{
    [Theory]
    [InlineData(1.5, 1.5)]            // double
    [InlineData(2, 2)]                // int
    [InlineData("3,14", 3.14)]        // string з комою (uk-UA)
    [InlineData("3.14", 3.14)]        // string з крапкою (invariant)
    public void TryParse_ValidValue_ReturnsTrue(object input, decimal expected)
    {
        var success = DecimalParser.TryParse(input, out var result);
        success.Should().BeTrue();
        result.Should().Be(expected);
    }
    [Theory]
    [InlineData("0.125", 0.13)]    // AwayFromZero: 0.13, ToEven дав би 0.12
    [InlineData("-0.125", -0.13)]
    [InlineData("0.135", 0.14)]
    public void TryParse_HalfwayValue_RoundsAwayFromZero(string input, double expectedDouble)
    {
        var expected = (decimal)expectedDouble;

        var success = DecimalParser.TryParse(input, out var result);

        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        var success = DecimalParser.TryParse(null, out var result);

        success.Should().BeFalse();
        result.Should().Be(0m);
    }

    [Fact]
    public void TryParse_DBNull_ReturnsFalse()
    {
        var success = DecimalParser.TryParse(DBNull.Value, out var result);

        success.Should().BeFalse();
        result.Should().Be(0m);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_Invalid_ReturnsFalse(string input)
    {
        var success = DecimalParser.TryParse(input, out var result);

        success.Should().BeFalse();
        result.Should().Be(0m);
    }

    [Theory]
    [InlineData(1E300)]                  // більше за decimal.MaxValue — каст кинув би Overflow
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void TryParse_HugeOrInvalidDouble_ReturnsFalseInsteadOfThrowing(double input)
    {
        // Контракт TryParse: кривa ячейка = false (помилка рядка у звіті), НЕ виняток на весь імпорт.
        var success = DecimalParser.TryParse(input, out var result);

        success.Should().BeFalse();
        result.Should().Be(0m);
    }
}
