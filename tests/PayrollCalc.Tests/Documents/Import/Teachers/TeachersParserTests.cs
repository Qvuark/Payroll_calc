using FluentAssertions;
using PayrollCalc.Documents.Import.Teachers;

namespace PayrollCalc.Tests.Documents.Import.Teachers;

public class TeachersParserTests
{
    [Fact]
    public void ValidSheet_ParsesAllRows_NoErrors()
    {
        // Arrange — одна повна валідна строка
        var sheet = TeachersSheetBuilder.BuildValid(TeachersSheetBuilder.ValidRow());
        var parser = new TeachersParser();
        // Act
        var (rows, errors) = parser.ParseSheet(sheet);
        // Assert
        errors.Should().BeEmpty();
        rows.Should().HaveCount(1);

        var dto = rows[0];
        dto.TabNumber.Should().Be("T001");
        dto.FullName.Should().Be("Іваненко Іван Іванович");
        dto.TaxId.Should().Be("1234567890");
        dto.HireDate.Should().Be(new DateOnly(2020, 9, 1));
        dto.Position.Should().Be("Вчитель");
        dto.Stavki.Should().Be(1.0m);
        dto.TariffGrade.Should().Be(12);
        // RowIndex = 3, бо row 1 = headers, row 2 = descriptions, row 3 = data (1-based для мами)
        dto.RowIndex.Should().Be(3);
    }
    // ─── Mandatory missing → error + DTO не побудований ───
    [Fact]
    public void MissingTabNumber_ErrorAndNoDto()
    {
        var row = TeachersSheetBuilder.ValidRow();
        row[TeachersColumnMap.ColTabNumber] = null;
        var sheet = TeachersSheetBuilder.BuildValid(row);

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Field.Should().Be("TabNumber");
    }
    [Fact]
    public void MissingHireDate_ErrorAndNoDto()
    {
        var row = TeachersSheetBuilder.ValidRow();
        row[TeachersColumnMap.ColHireDate] = null;
        var sheet = TeachersSheetBuilder.BuildValid(row);

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Field.Should().Be("HireDate");
    }
    // ─── TaxId format check (10 цифр) ───
    [Fact]
    public void InvalidTaxId_NotTenDigits_ErrorAndNoDto()
    {
        var row = TeachersSheetBuilder.ValidRow();
        row[TeachersColumnMap.ColTaxId] = "123";
        var sheet = TeachersSheetBuilder.BuildValid(row);

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Field.Should().Be("TaxId");
    }
    [Fact]
    public void InvalidTaxId_Letters_ErrorAndNoDto()
    {
        var row = TeachersSheetBuilder.ValidRow();
        row[TeachersColumnMap.ColTaxId] = "abcd123456";
        var sheet = TeachersSheetBuilder.BuildValid(row);

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle()
            .Which.Field.Should().Be("TaxId");
    }
    // ─── Skip empty row ───
    [Fact]
    public void EmptyRowInMiddle_IsSkippedWithoutError()
    {
        // Валідна → пуста → валідна. Пуста має тихо пропуститись.
        var sheet = TeachersSheetBuilder.BuildValid(
            TeachersSheetBuilder.ValidRow(),
            TeachersSheetBuilder.EmptyRow(),
            TeachersSheetBuilder.ValidRow());

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        rows.Should().HaveCount(2);
        errors.Should().BeEmpty();
    }
    // ─── Sheet без даних ───
    [Fact]
    public void EmptySheet_OnlyHeaders_ReturnsError()
    {
        // BuildValid без data rows → тільки headers + descriptions
        var sheet = TeachersSheetBuilder.BuildValid();

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("відсутні дані");
    }
    // ─── Optional невалідний → error, але DTO будується ───
    [Fact]
    public void InvalidOptionalDecimal_ErrorButDtoBuilt()
    {
        var row = TeachersSheetBuilder.ValidRow();
        row[TeachersColumnMap.ColComplexityPct] = "abc";
        var sheet = TeachersSheetBuilder.BuildValid(row);

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        // DTO будується (mandatory всі ок), але optional поле = null + error в звіті
        rows.Should().HaveCount(1);
        rows[0].ComplexityPct.Should().BeNull();
        errors.Should().ContainSingle().Which.Field.Should().Be("ComplexityPct");
    }
    // ─── Всі optional пусті → нуль помилок ───
    [Fact]
    public void EmptyOptionalFields_NoErrors()
    {
        // ValidRow() заповнює тільки mandatory, всі optional → null/0
        var sheet = TeachersSheetBuilder.BuildValid(TeachersSheetBuilder.ValidRow());

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows.Should().HaveCount(1);
        var dto = rows[0];
        dto.Education.Should().BeNull();
        dto.ComplexityPct.Should().BeNull();
        dto.IsHonored.Should().BeFalse();
    }
    // ─── Hours: пусто → 0m (не null, бо decimal non-nullable) ───
    [Fact]
    public void HoursEmpty_DefaultZero()
    {
        var sheet = TeachersSheetBuilder.BuildValid(TeachersSheetBuilder.ValidRow());

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows[0].Hours1To4.Should().Be(0m);
        rows[0].IndividualHours5To9.Should().Be(0m);
    }
    [Fact]
    public void HoursInvalid_ErrorAndZero()
    {
        var row = TeachersSheetBuilder.ValidRow();
        row[TeachersColumnMap.ColHours1To4] = "abc";
        var sheet = TeachersSheetBuilder.BuildValid(row);

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        rows.Should().HaveCount(1);
        rows[0].Hours1To4.Should().Be(0m);
        errors.Should().ContainSingle().Which.Field.Should().Be("Hours1To4");
    }
    // ─── Cross-record не справа Parser'а: дві позиції на один TaxId — обидві DTO ───
    [Fact]
    public void MultiplePositionsForSameTaxId_AllParsed()
    {
        // Parser не знає про cross-record. Дві строки одного TaxId з різними посадами
        // → дві DTO, нуль помилок. Дедуплікація — у Importer (vault: importer_edge_cases.md).
        var row1 = TeachersSheetBuilder.ValidRow();
        var row2 = TeachersSheetBuilder.ValidRow();
        row2[TeachersColumnMap.ColPosition] = "Вихователь";

        var sheet = TeachersSheetBuilder.BuildValid(row1, row2);

        var (rows, errors) = new TeachersParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows.Should().HaveCount(2);
        rows[0].Position.Should().Be("Вчитель");
        rows[1].Position.Should().Be("Вихователь");
    }
}
