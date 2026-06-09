using ClosedXML.Excel;
using FluentAssertions;
using PayrollCalc.Calculation;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Export.Vedomost;

namespace PayrollCalc.Tests.Documents;

/// <summary>
/// Перевіряє, що відомість будується з результатів рушія в правильні колонки:
/// J/N-роздвоєння окладу за класом, додавання надбавок багатоставкового в одну клітинку,
/// живі підсумкові формули, шапка та рядок «Разом».
/// </summary>
public class VedomostExporterTests
{
    private static readonly PayrollParams Params = new()
    {
        Pdfo = 0.18m, Vz = 0.05m, Union = 0.01m, Bonus1749 = 0.40m,
        Mzp = 8647m, UnfavorableBase = 2600m, Disinfectants = 0.10m, NightShifts = 0.40m,
    };

    [Fact]
    public void Build_DirectorPlusTeacher_SplitsJnAndSumsSharedColumns()
    {
        // Скирда: директор (J-гілка) + вчитель (N-гілка), обидва з престижністю → колонка R = сума.
        var director = new PositionCalcInput
        {
            WorkerClass = WorkerClass.AdminPedagogical, PositionName = "Директор",
            Oklad = 10410m, RateCount = 1m, TenurePct = 0.30m, PrestigePct = 0.25m,
        };
        var teacher = new PositionCalcInput
        {
            WorkerClass = WorkerClass.Pedagogical, PositionName = "Вчитель",
            Oklad = 8397m, RateCount = 1m, PedHoursWeekly = 9m, TitlePct = 0.15m,
            TenurePct = 0.30m, PrestigePct = 0.20m,
        };
        var result = Calc(director, teacher);

        using var wb = Load(new VedomostExporter().Build([result], 2026, 3));
        var ws = wb.Worksheet("ведомость");

        ws.Cell("J2").GetString().Should().Be("Посадовий оклад");
        ws.Cell("N2").GetString().Should().Be("Оклад педагогів");

        // Оклад роздвоївся: директор → J, вчитель → N.
        ws.Cell("J3").FormulaA1.Should().NotBeEmpty();
        ws.Cell("N3").FormulaA1.Should().NotBeEmpty();

        // Престижність обох ставок зведена в одну клітинку R (директор 25% + вчитель 20%).
        var r3 = ws.Cell("R3").FormulaA1;
        r3.Should().Contain("25%");
        r3.Should().Contain("20%");

        // Підсумки — живі формули.
        ws.Cell("BC3").FormulaA1.Should().Be("SUM(J3:BB3)");
        ws.Cell("BD3").FormulaA1.Should().Be("BC3*18%");
        ws.Cell("BK3").FormulaA1.Should().Be("BC3-BJ3");

        // Рядок «Разом» одразу під єдиним працівником (рядок 4).
        ws.Cell("B4").GetString().Should().Be("Разом");
        ws.Cell("BC4").FormulaA1.Should().Be("SUM(BC3:BC3)");
    }

    [Fact]
    public void Build_UnknownComponentColumn_Throws()
    {
        // Захист: надбавка без колонки = мовчазна втрата в gross, має кинути.
        var result = new CalcResult
        {
            EmployeeId = 1, FullName = "Тест", Year = 2026, Month = 3,
            Earnings = [new CalcComponent("Невідома надбавка", 100m, "=100")],
            Gross = 100m, Deductions = [], TotalWithheld = 0m, NetPay = 100m,
            ParamsSnapshot = new Dictionary<string, decimal>(),
        };

        var act = () => new VedomostExporter().Build([result], 2026, 3);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Невідома надбавка*");
    }

    private static CalcResult Calc(params PositionCalcInput[] positions)
        => new PayrollCalculator().Calculate(new CalcInput
        {
            EmployeeId = 1, FullName = "Скирда Г. Ф.", Year = 2026, Month = 3,
            NormDays = 22, WorkedDays = 22, Positions = positions,
            Manual = new ManualAdjustments(), Params = Params,
        });

    private static XLWorkbook Load(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        return new XLWorkbook(ms);
    }
}
