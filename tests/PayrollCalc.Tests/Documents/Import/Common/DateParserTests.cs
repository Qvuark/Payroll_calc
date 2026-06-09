using FluentAssertions;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Tests.Documents.Import.Common;

public class DateParserTests
{
    [Theory]
    [InlineData("12.05.2024", 2024, 5, 12)]
    [InlineData("2024-05-12", 2024, 5, 12)]
    [InlineData("12/05/2024", 2024, 5, 12)]
    [InlineData("1.5.2024", 2024, 5, 1)]
    [InlineData("12.05.24", 2024, 5, 12)]
    public void TryParse_ValidString_ReturnsDate(string input, int year, int month, int day)
    {
        var success = DateParser.TryParse(input, out var result);

        success.Should().BeTrue();
        result.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    public void TryParse_DateTime_ReturnsDateOnly()
    {
        var input = new DateTime(2024, 5, 12, 10, 30, 0);

        var success = DateParser.TryParse(input, out var result);

        success.Should().BeTrue();
        result.Should().Be(new DateOnly(2024, 5, 12));
    }

    [Fact]
    public void TryParse_ExcelSerial_Double_ReturnsDate()
    {
        // OADate: 2024-05-12 = serial 45424
        var serial = new DateTime(2024, 5, 12).ToOADate();

        var success = DateParser.TryParse(serial, out var result);

        success.Should().BeTrue();
        result.Should().Be(new DateOnly(2024, 5, 12));
    }

    [Fact]
    public void TryParse_Null_ReturnsFalse()
    {
        var success = DateParser.TryParse(null, out var result);

        success.Should().BeFalse();
        result.Should().Be(default);
    }

    [Fact]
    public void TryParse_DBNull_ReturnsFalse()
    {
        var success = DateParser.TryParse(DBNull.Value, out var result);

        success.Should().BeFalse();
        result.Should().Be(default);
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("32.13.2024")]
    [InlineData("2024/05/12")]
    public void TryParse_Invalid_ReturnsFalse(string input)
    {
        var success = DateParser.TryParse(input, out var result);

        success.Should().BeFalse();
        result.Should().Be(default);
    }

    [Theory]
    [InlineData(9999999.0)]    // за межами OADate — FromOADate кинув би ArgumentException
    [InlineData(-9999999.0)]
    public void TryParse_DoubleOutsideOADateRange_ReturnsFalseInsteadOfThrowing(double input)
    {
        var success = DateParser.TryParse(input, out var result);

        success.Should().BeFalse();
        result.Should().Be(default);
    }
}
