using ClosedXML.Excel;
using FluentAssertions;
using PayrollCalc.Calculation;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Export.Payslip;

namespace PayrollCalc.Tests.Documents;

/// <summary>
/// Перевіряє розкладку розрахункових листів: дві платіжки в ряд (ліва B-E, права F-I),
/// шапка, секції Нараховано/Утримано, підсумок «Сума до видачі».
/// </summary>
public class PayslipExporterTests
{
    private static readonly PayrollParams Params = new()
    {
        Pdfo = 0.18m, Vz = 0.05m, Union = 0.01m, Bonus1749 = 0.40m,
        Mzp = 8647m, UnfavorableBase = 2600m, Disinfectants = 0.10m, NightShifts = 0.40m,
    };

    [Fact]
    public void Build_TwoEmployees_RendersSideBySide()
    {
        var teacher = Teacher("Вдовченко Т. В.", "2334902602");
        var spec = Spec("Костенко Л. П.", "1112223334");

        using var wb = Load(new PayslipExporter().Build([teacher, spec], 2026, 3));
        var ws = wb.Worksheet("розрахунковий лист");

        // Ліва платіжка в B, права в F, в одному ряду.
        ws.Cell("B1").GetString().Should().Contain("Розрахунковий лист");
        ws.Cell("F1").GetString().Should().Contain("Розрахунковий лист");
        ws.Cell("B2").GetString().Should().Be("Вдовченко Т. В.");
        ws.Cell("F2").GetString().Should().Be("Костенко Л. П.");
        ws.Cell("B3").GetString().Should().Contain("2334902602");

        // Секції є з обох боків.
        UsedStrings(ws, "B").Should().Contain("Нараховано").And.Contain("Сума до видачі");
        UsedStrings(ws, "D").Should().Contain("Утримано");
        UsedStrings(ws, "F").Should().Contain("Нараховано");
    }

    [Fact]
    public void Build_OddCount_LastHasNoRightColumn()
    {
        using var wb = Load(new PayslipExporter().Build([Teacher("Один О.", "1")], 2026, 3));
        var ws = wb.Worksheet("розрахунковий лист");

        ws.Cell("B2").GetString().Should().Be("Один О.");
        ws.Cell("F1").GetString().Should().BeEmpty();   // правої платіжки немає
    }

    private static CalcResult Teacher(string name, string taxId) => new PayrollCalculator().Calculate(new CalcInput
    {
        EmployeeId = 1, FullName = name, TaxId = taxId, Year = 2026, Month = 3, NormDays = 22, WorkedDays = 22,
        Positions = [new PositionCalcInput
        {
            WorkerClass = WorkerClass.Pedagogical, PositionName = "Вчитель", Oklad = 8397m,
            RateCount = 1m, PedHoursWeekly = 9m, TitlePct = 0.15m, TenurePct = 0.30m, PrestigePct = 0.20m,
        }],
        Manual = new ManualAdjustments(), Params = Params,
    });

    private static CalcResult Spec(string name, string taxId) => new PayrollCalculator().Calculate(new CalcInput
    {
        EmployeeId = 2, FullName = name, TaxId = taxId, Year = 2026, Month = 3, NormDays = 22, WorkedDays = 22,
        Positions = [new PositionCalcInput
        {
            WorkerClass = WorkerClass.Specialist, PositionName = "Бібліотекар", Oklad = 7356m, RateCount = 1m,
            HasLibrarianTenure = true,
        }],
        Manual = new ManualAdjustments(), Params = Params,
    });

    private static IEnumerable<string> UsedStrings(IXLWorksheet ws, string column)
        => ws.Column(column).CellsUsed().Select(c => c.GetString());

    private static XLWorkbook Load(byte[] bytes) => new(new MemoryStream(bytes));
}
