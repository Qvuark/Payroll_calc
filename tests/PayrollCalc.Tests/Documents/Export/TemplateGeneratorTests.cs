using ClosedXML.Excel;
using FluentAssertions;
using PayrollCalc.Documents.Export;
using PayrollCalc.Documents.Import.Staff;
using PayrollCalc.Documents.Import.Teachers;
using PayrollCalc.Documents.Import.Timesheet;

namespace PayrollCalc.Tests.Documents.Export;

public class TemplateGeneratorTests
{
    // ─── Content: headers + descriptions потрапили на правильні рядки/колонки ───
    [Fact]
    public void Generate_TeachersMap_WritesEnglishHeadersOnHeaderRow()
    {
        var map = new TeachersColumnMap();
        var bytes = new TemplateGenerator().Generate(map);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        foreach (var (col, expected) in map.ExpectedHeaders)
            ws.Cell(map.HeaderRowIndex + 1, col + 1).GetString().Should().Be(expected);
    }

    [Fact]
    public void Generate_StaffMap_WritesUkrainianDescriptionsOnDescRow()
    {
        var map = new StaffColumnMap();
        var bytes = new TemplateGenerator().Generate(map);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        foreach (var (col, expected) in map.Descriptions)
            ws.Cell(map.DescriptionRowIndex + 1, col + 1).GetString().Should().Be(expected);
    }

    // ─── Style: header bold, description fill кольором ───
    [Fact]
    public void Generate_HeaderRow_IsBold()
    {
        var map = new TeachersColumnMap();
        var bytes = new TemplateGenerator().Generate(map);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        var firstHeaderCol = map.ExpectedHeaders.Keys.First();
        ws.Cell(map.HeaderRowIndex + 1, firstHeaderCol + 1)
            .Style.Font.Bold.Should().BeTrue();
    }

    [Fact]
    public void Generate_DescriptionRow_HasBackgroundColor()
    {
        var map = new TeachersColumnMap();
        var bytes = new TemplateGenerator().Generate(map);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        var firstDescCol = map.Descriptions.Keys.First();
        // Не білий = фон проставлений (XLColor.NoColor серіалізується як білий FFFFFFFF).
        ws.Cell(map.DescriptionRowIndex + 1, firstDescCol + 1)
            .Style.Fill.BackgroundColor.Color.Name.Should().NotBe("White");
    }

    // ─── Sheet name: default + custom ───
    [Fact]
    public void Generate_DefaultSheetName_IsSheet1()
    {
        var bytes = new TemplateGenerator().Generate(new TeachersColumnMap());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        wb.Worksheets.First().Name.Should().Be("Sheet1");
    }

    [Fact]
    public void Generate_CustomSheetName_IsUsed()
    {
        var bytes = new TemplateGenerator().Generate(new TeachersColumnMap(), "Вчителі");

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        wb.Worksheets.First().Name.Should().Be("Вчителі");
    }

    // ─── Round-trip: згенерований шаблон проходить header-валідацію парсера ───
    // Це найважливіший тест — ловить розузгодження між мапою генератора і мапою парсера
    // (наприклад якщо хтось випадково підмінить порядок колонок або заголовок).
    [Fact]
    public void Generate_RoundTrip_TeachersParserAcceptsHeaders()
    {
        var bytes = new TemplateGenerator().Generate(new TeachersColumnMap());

        var (rows, errors) = new TeachersParser().Parse(new MemoryStream(bytes));

        // Header validation OK → парсер доходить до перевірки даних.
        // Даних нема → одна помилка "відсутні дані", НЕ header errors.
        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("відсутні дані");
    }

    [Fact]
    public void Generate_RoundTrip_StaffParserAcceptsHeaders()
    {
        var bytes = new TemplateGenerator().Generate(new StaffColumnMap());

        var (rows, errors) = new StaffParser().Parse(new MemoryStream(bytes));

        rows.Should().BeEmpty();
        errors.Should().ContainSingle().Which.Message.Should().Contain("відсутні дані");
    }

    // ─── FooterNotes: легенда timesheet пишеться на лист ───
    [Fact]
    public void Generate_TimesheetMap_WritesFooterNotes()
    {
        var map = new TimesheetColumnMap();
        var bytes = new TemplateGenerator().Generate(map);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        var firstNote = map.FooterNotes.First();
        ws.CellsUsed().Any(c => c.GetString() == firstNote).Should().BeTrue();
    }

    // ─── Pre-fill: значення рядка потрапляє у клітинку + сірий фон ("не чіпати") ───
    [Fact]
    public void Generate_PrefillRow_WritesValueWithGrayFill()
    {
        var map = new TimesheetColumnMap();
        var rows = new List<IReadOnlyDictionary<int, string>>
        {
            new Dictionary<int, string> { { TimesheetColumnMap.ColTaxId, "1234567890" } },
        };
        var bytes = new TemplateGenerator().Generate(map, rows);

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        var cell = ws.Cell(map.FirstDataRowIndex + 1, TimesheetColumnMap.ColTaxId + 1);
        cell.GetString().Should().Be("1234567890");
        // Сірий фон prefill ≠ білий (NoColor серіалізується як White).
        cell.Style.Fill.BackgroundColor.Color.Name.Should().NotBe("White");
    }
}
