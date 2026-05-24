using FluentAssertions;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Tests.Documents.Import.Common;

public class BoolParserTests
{
    [Theory]
    [InlineData("так", true)]
    [InlineData("Так", true)]
    [InlineData("ТАК", true)]
    [InlineData("да", true)]
    [InlineData("yes", true)]
    [InlineData("YES", true)]
    [InlineData("y", true)]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("+", true)]
    [InlineData("ні", false)]
    [InlineData("Ні", false)]
    [InlineData("нет", false)]
    [InlineData("no", false)]
    [InlineData("n", false)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData("-", false)]
    [InlineData("", false)]
    public void TryParse_KnownString_ReturnsExpected(string input, bool expected)
    {
        var success = BoolParser.TryParse(input, out var result);

        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Fact]
    public void TryParse_BoolTrue_ReturnsTrue()
    {
        var success = BoolParser.TryParse(true, out var result);

        success.Should().BeTrue();
        result.Should().BeTrue();
    }

    [Fact]
    public void TryParse_BoolFalse_ReturnsFalse()
    {
        var success = BoolParser.TryParse(false, out var result);

        success.Should().BeTrue();
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(-5, true)]
    [InlineData(0, false)]
    public void TryParse_Int_ReturnsBasedOnZero(int input, bool expected)
    {
        var success = BoolParser.TryParse(input, out var result);

        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        var success = BoolParser.TryParse(null, out var result);

        success.Should().BeFalse();
        result.Should().BeFalse();
    }

    [Fact]
    public void TryParse_DBNull_ReturnsFalse()
    {
        var success = BoolParser.TryParse(DBNull.Value, out var result);

        success.Should().BeFalse();
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("maybe")]
    [InlineData("xyz")]
    [InlineData("2")]
    public void TryParse_UnknownString_ReturnsFalse(string input)
    {
        var success = BoolParser.TryParse(input, out var result);

        success.Should().BeFalse();
        result.Should().BeFalse();
    }
}
