using FluentAssertions;
using PayrollCalc.Documents.Import.Timesheet;

namespace PayrollCalc.Tests.Documents.Import.Timesheet;

/// <summary>
/// Unit-тести TimesheetParser: DataTable → (rows, errors) без БД.
/// Match-ключ = TaxId; пустий TaxId = skip-empty; криве число → error + 0, DTO будується.
/// </summary>
public class TimesheetParserTests
{
    [Fact]
    public void ValidSheet_ParsesRow_NoErrors()
    {
        var sheet = TimesheetSheetBuilder.BuildValid(TimesheetSheetBuilder.ValidRow());

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows.Should().ContainSingle();
        var dto = rows[0];
        dto.TaxId.Should().Be("9876543210");
        dto.WorkedDays.Should().Be(20m);
        dto.ReplacementHours.Should().Be(5m);
        dto.NightHours.Should().Be(8m);
        // row 1 = headers, row 2 = descriptions, row 3 = data (1-based для мами)
        dto.RowIndex.Should().Be(3);
    }

    // ─── Skip-empty: пустий TaxId (match-ключ) → тихо пропускаємо, не помилка ───
    [Fact]
    public void BlankTaxId_RowSkippedSilently()
    {
        var row = TimesheetSheetBuilder.ValidRow();
        row[TimesheetColumnMap.ColTaxId] = null;
        var sheet = TimesheetSheetBuilder.BuildValid(row);

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void EmptyRowInMiddle_IsSkippedWithoutError()
    {
        var sheet = TimesheetSheetBuilder.BuildValid(
            TimesheetSheetBuilder.ValidRow(),
            TimesheetSheetBuilder.EmptyRow(),
            TimesheetSheetBuilder.ValidRow());

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        rows.Should().HaveCount(2);
        errors.Should().BeEmpty();
    }

    // ─── TaxId format (10 цифр) ───
    [Fact]
    public void InvalidTaxId_NotTenDigits_ErrorAndNoDto()
    {
        var row = TimesheetSheetBuilder.ValidRow();
        row[TimesheetColumnMap.ColTaxId] = "123";
        var sheet = TimesheetSheetBuilder.BuildValid(row);

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Field.Should().Be("TaxId");
    }

    [Fact]
    public void InvalidTaxId_Letters_ErrorAndNoDto()
    {
        var row = TimesheetSheetBuilder.ValidRow();
        row[TimesheetColumnMap.ColTaxId] = "abcd123456";
        var sheet = TimesheetSheetBuilder.BuildValid(row);

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Field.Should().Be("TaxId");
    }

    // ─── Sheet без даних ───
    [Fact]
    public void EmptySheet_OnlyHeaders_ReturnsError()
    {
        var sheet = TimesheetSheetBuilder.BuildValid();

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("відсутні дані");
    }

    // ─── Числа: пусто → 0 (decimal non-nullable, 0 = валідне значення) ───
    [Fact]
    public void EmptyNumbers_DefaultZero()
    {
        var row = TimesheetSheetBuilder.ValidRow();
        row[TimesheetColumnMap.ColWorkedDays] = null;
        row[TimesheetColumnMap.ColReplacementHours] = null;
        row[TimesheetColumnMap.ColNightHours] = null;
        var sheet = TimesheetSheetBuilder.BuildValid(row);

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows.Should().ContainSingle();
        rows[0].WorkedDays.Should().Be(0m);
        rows[0].ReplacementHours.Should().Be(0m);
        rows[0].NightHours.Should().Be(0m);
    }

    // ─── Криве число → error + 0, але DTO будується (TaxId валідний) ───
    [Fact]
    public void InvalidNumber_ErrorAndZeroButDtoBuilt()
    {
        var row = TimesheetSheetBuilder.ValidRow();
        row[TimesheetColumnMap.ColWorkedDays] = "abc";
        var sheet = TimesheetSheetBuilder.BuildValid(row);

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        rows.Should().ContainSingle();
        rows[0].WorkedDays.Should().Be(0m);
        errors.Should().ContainSingle().Which.Field.Should().Be("WorkedDays");
    }

    // ─── Дробове число днів парситься; норму перевіряє Importer, не Parser ───
    [Fact]
    public void FractionalWorkedDays_Parsed()
    {
        var row = TimesheetSheetBuilder.ValidRow();
        row[TimesheetColumnMap.ColWorkedDays] = 20.5;
        var sheet = TimesheetSheetBuilder.BuildValid(row);

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows[0].WorkedDays.Should().Be(20.5m);
    }

    // ─── Дві строки різних ІПН — обидві DTO (cross-record не справа Parser'а) ───
    [Fact]
    public void TwoRows_BothParsed()
    {
        var row1 = TimesheetSheetBuilder.ValidRow();
        var row2 = TimesheetSheetBuilder.ValidRow();
        row2[TimesheetColumnMap.ColTaxId] = "1111111111";
        var sheet = TimesheetSheetBuilder.BuildValid(row1, row2);

        var (rows, errors) = new TimesheetParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows.Should().HaveCount(2);
        rows[1].TaxId.Should().Be("1111111111");
    }
}
