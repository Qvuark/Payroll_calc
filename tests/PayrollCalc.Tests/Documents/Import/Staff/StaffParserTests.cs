using FluentAssertions;
using PayrollCalc.Documents.Import.Staff;

namespace PayrollCalc.Tests.Documents.Import.Staff;

public class StaffParserTests
{
    [Fact]
    public void ValidSheet_ParsesAllRows_NoErrors()
    {
        // Arrange — одна повна валідна строка
        var sheet = StaffSheetBuilder.BuildValid(StaffSheetBuilder.ValidRow());
        var parser = new StaffParser();
        // Act
        var (rows, errors) = parser.ParseSheet(sheet);
        // Assert
        errors.Should().BeEmpty();
        rows.Should().HaveCount(1);

        var dto = rows[0];
        dto.TabNumber.Should().Be("S001");
        dto.FullName.Should().Be("Сидоренко Анна Іванівна");
        dto.TaxId.Should().Be("9876543210");
        dto.HireDate.Should().Be(new DateOnly(2020, 9, 1));
        dto.Position.Should().Be("Бухгалтер");
        dto.Stavki.Should().Be(1.0m);
        dto.TariffGrade.Should().Be(12);
        // RowIndex = 3, бо row 1 = headers, row 2 = descriptions, row 3 = data (1-based для мами)
        dto.RowIndex.Should().Be(3);
    }
    // ─── Mandatory missing → error + DTO не побудований ───
    [Fact]
    public void MissingTabNumber_ErrorAndNoDto()
    {
        var row = StaffSheetBuilder.ValidRow();
        row[StaffColumnMap.ColTabNumber] = null;
        var sheet = StaffSheetBuilder.BuildValid(row);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Field.Should().Be("TabNumber");
    }
    [Fact]
    public void MissingHireDate_ErrorAndNoDto()
    {
        var row = StaffSheetBuilder.ValidRow();
        row[StaffColumnMap.ColHireDate] = null;
        var sheet = StaffSheetBuilder.BuildValid(row);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Field.Should().Be("HireDate");
    }
    // ─── TaxId format check (10 цифр) ───
    [Fact]
    public void InvalidTaxId_NotTenDigits_ErrorAndNoDto()
    {
        var row = StaffSheetBuilder.ValidRow();
        row[StaffColumnMap.ColTaxId] = "123";
        var sheet = StaffSheetBuilder.BuildValid(row);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Field.Should().Be("TaxId");
    }
    [Fact]
    public void InvalidTaxId_Letters_ErrorAndNoDto()
    {
        var row = StaffSheetBuilder.ValidRow();
        row[StaffColumnMap.ColTaxId] = "abcd123456";
        var sheet = StaffSheetBuilder.BuildValid(row);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle()
            .Which.Field.Should().Be("TaxId");
    }
    // ─── Skip empty row ───
    [Fact]
    public void EmptyRowInMiddle_IsSkippedWithoutError()
    {
        // Валідна → пуста → валідна. Пуста має тихо пропуститись.
        var sheet = StaffSheetBuilder.BuildValid(
            StaffSheetBuilder.ValidRow(),
            StaffSheetBuilder.EmptyRow(),
            StaffSheetBuilder.ValidRow());

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        rows.Should().HaveCount(2);
        errors.Should().BeEmpty();
    }
    // ─── Sheet без даних ───
    [Fact]
    public void EmptySheet_OnlyHeaders_ReturnsError()
    {
        // BuildValid без data rows → тільки headers + descriptions
        var sheet = StaffSheetBuilder.BuildValid();

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("відсутні дані");
    }
    // ─── Optional невалідний → error, але DTO будується ───
    [Fact]
    public void InvalidOptionalDecimal_ErrorButDtoBuilt()
    {
        var row = StaffSheetBuilder.ValidRow();
        row[StaffColumnMap.ColComplexityPct] = "abc";
        var sheet = StaffSheetBuilder.BuildValid(row);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        // DTO будується (mandatory всі ок), але optional поле = null + error в звіті
        rows.Should().HaveCount(1);
        rows[0].ComplexityPct.Should().BeNull();
        errors.Should().ContainSingle().Which.Field.Should().Be("ComplexityPct");
    }
    // ─── Всі optional пусті → нуль помилок ───
    [Fact]
    public void EmptyOptionalFields_NoErrors()
    {
        // ValidRow() заповнює тільки mandatory, всі optional → null/0/false
        var sheet = StaffSheetBuilder.BuildValid(StaffSheetBuilder.ValidRow());

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows.Should().HaveCount(1);
        var dto = rows[0];
        dto.Education.Should().BeNull();
        dto.ComplexityPct.Should().BeNull();
        dto.IsHonored.Should().BeFalse();
        dto.MentorAmount.Should().BeNull();
        dto.NightShifts.Should().BeFalse();
    }
    // ─── Hours: пусто → 0m (не null, бо decimal non-nullable) ───
    [Fact]
    public void GpdPkrHoursEmpty_DefaultZero()
    {
        var sheet = StaffSheetBuilder.BuildValid(StaffSheetBuilder.ValidRow());

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows[0].GpdRate.Should().Be(0m);
        rows[0].PkrHours.Should().Be(0m);
    }
    [Fact]
    public void GpdRateInvalid_ErrorAndZero()
    {
        var row = StaffSheetBuilder.ValidRow();
        row[StaffColumnMap.ColGpdRate] = "abc";
        var sheet = StaffSheetBuilder.BuildValid(row);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        rows.Should().HaveCount(1);
        rows[0].GpdRate.Should().Be(0m);
        errors.Should().ContainSingle().Which.Field.Should().Be("GpdRate");
    }
    // ─── Bool: "так"/"ні" парсяться через BoolParser ───
    [Fact]
    public void DisinfectantsTrue_BoolParsed()
    {
        var row = StaffSheetBuilder.ValidRow();
        row[StaffColumnMap.ColDisinfectants] = "так";
        row[StaffColumnMap.ColNightShifts] = "ні";
        var sheet = StaffSheetBuilder.BuildValid(row);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows[0].Disinfectants.Should().BeTrue();
        rows[0].NightShifts.Should().BeFalse();
    }
    // ─── Mentor amount: значення зберігається, 0 ≠ null ───
    [Fact]
    public void MentorAmount_ZeroIsNotNull()
    {
        var row = StaffSheetBuilder.ValidRow();
        row[StaffColumnMap.ColMentorAmount] = 0;
        var sheet = StaffSheetBuilder.BuildValid(row);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        // Важливо: 0 grn призначено ≠ не призначено. Тест ловить регресії якщо
        // помилково замінять GetOptionalDecimal на GetOptionalHours для amounts.
        rows[0].MentorAmount.Should().Be(0m);
    }
    // ─── Cross-record не справа Parser'а: дві позиції на один TaxId — обидві DTO ───
    [Fact]
    public void MultiplePositionsForSameTaxId_AllParsed()
    {
        // Parser не знає про cross-record. Дві строки одного TaxId з різними посадами
        // → дві DTO, нуль помилок. Дедуплікація — у Importer (vault: importer_edge_cases.md).
        var row1 = StaffSheetBuilder.ValidRow();
        var row2 = StaffSheetBuilder.ValidRow();
        row2[StaffColumnMap.ColPosition] = "Завідувач бібліотеки";

        var sheet = StaffSheetBuilder.BuildValid(row1, row2);

        var (rows, errors) = new StaffParser().ParseSheet(sheet);

        errors.Should().BeEmpty();
        rows.Should().HaveCount(2);
        rows[0].Position.Should().Be("Бухгалтер");
        rows[1].Position.Should().Be("Завідувач бібліотеки");
    }
}
