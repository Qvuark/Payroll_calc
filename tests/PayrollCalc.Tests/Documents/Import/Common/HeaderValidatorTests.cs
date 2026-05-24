using System.Data;
using FluentAssertions;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Tests.Documents.Import.Common;

public class HeaderValidatorTests
{
    private static DataTable BuildSheet(params string?[] headers)
    {
        var dt = new DataTable();
        for (int i = 0; i < headers.Length; i++)
            dt.Columns.Add($"Col{i}");
        dt.Rows.Add(headers.Cast<object?>().ToArray());
        return dt;
    }

    [Fact]
    public void Validate_AllHeadersMatch_ReturnsEmptyList()
    {
        var sheet = BuildSheet("TabNumber", "FullName", "TaxId");
        var expected = new Dictionary<int, string>
        {
            { 0, "TabNumber" }, { 1, "FullName" }, { 2, "TaxId" }
        };

        var errors = HeaderValidator.Validate(sheet, headerRowIndex: 0, expected);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_CaseInsensitive_ReturnsEmpty()
    {
        var sheet = BuildSheet("tabnumber", "FULLNAME");
        var expected = new Dictionary<int, string>
        {
            { 0, "TabNumber" }, { 1, "FullName" }
        };

        var errors = HeaderValidator.Validate(sheet, 0, expected);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_HeaderMismatch_ReturnsError()
    {
        var sheet = BuildSheet("TabNum", "FullName");
        var expected = new Dictionary<int, string>
        {
            { 0, "TabNumber" }, { 1, "FullName" }
        };

        var errors = HeaderValidator.Validate(sheet, 0, expected);

        errors.Should().HaveCount(1);
        errors[0].Message.Should().Contain("TabNumber").And.Contain("TabNum");
    }

    [Fact]
    public void Validate_MissingColumn_ReturnsError()
    {
        var sheet = BuildSheet("TabNumber", "FullName");
        var expected = new Dictionary<int, string>
        {
            { 0, "TabNumber" }, { 1, "FullName" }, { 2, "TaxId" }
        };

        var errors = HeaderValidator.Validate(sheet, 0, expected);

        errors.Should().HaveCount(1);
        errors[0].Message.Should().Contain("Бракує");
    }

    [Fact]
    public void Validate_EmptyHeaderCell_ReturnsError()
    {
        var sheet = BuildSheet("TabNumber", null);
        var expected = new Dictionary<int, string>
        {
            { 0, "TabNumber" }, { 1, "FullName" }
        };

        var errors = HeaderValidator.Validate(sheet, 0, expected);

        errors.Should().HaveCount(1);
        errors[0].Field.Should().Be("1");
    }

    [Fact]
    public void Validate_NoHeaderRow_ReturnsError()
    {
        var sheet = new DataTable();
        sheet.Columns.Add("Col0");
        var expected = new Dictionary<int, string> { { 0, "TabNumber" } };

        var errors = HeaderValidator.Validate(sheet, 0, expected);

        errors.Should().HaveCount(1);
        errors[0].Message.Should().Contain("не містить");
    }

    [Fact]
    public void Validate_TrimsWhitespace_ReturnsEmpty()
    {
        var sheet = BuildSheet("  TabNumber  ", " FullName ");
        var expected = new Dictionary<int, string>
        {
            { 0, "TabNumber" }, { 1, "FullName" }
        };

        var errors = HeaderValidator.Validate(sheet, 0, expected);

        errors.Should().BeEmpty();
    }
}
