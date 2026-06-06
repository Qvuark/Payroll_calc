using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.API.Application.Import;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Export;
using PayrollCalc.Documents.Import.Timesheet;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Integration-тести TimesheetTemplateService: реальний Postgres. Перевіряють що навантаження
/// з блоку Workload потрапляє у ПРАВИЛЬНІ сірі довідкові колонки згенерованого xlsx.
/// </summary>
public class TimesheetTemplateServiceTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fx;
    private const string Tax = "9876543210";

    public TimesheetTemplateServiceTests(PostgresFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetEmployeeDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Вчитель з навантаженням → сірі колонки заповнені своїми годинами, порожні групи лишаються пусті,
    /// ставки у колонці RateCount. Ловить розузгодження поле Workload → індекс колонки.
    /// </summary>
    [Fact]
    public async Task Fills_workload_into_correct_gray_columns()
    {
        await SeedTeacherWithLoadAsync();
        await using var db = _fx.CreateContext();
        var service = new TimesheetTemplateService(db, new TemplateGenerator());

        var bytes = await service.BuildAsync(2026, 3);

        var map = new TimesheetColumnMap();
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        var dataRow = map.FirstDataRowIndex + 1; // 1-based перший рядок даних

        string Cell(int col) => ws.Cell(dataRow, col + 1).GetString();

        // ідентифікація
        Cell(TimesheetColumnMap.ColTaxId).Should().Be(Tax);
        // навантаження легло у свої колонки
        Cell(TimesheetColumnMap.ColTariff5To9).Should().Be("18");
        Cell(TimesheetColumnMap.ColTariff10To11).Should().Be("6");
        Cell(TimesheetColumnMap.ColTariffInd1To4).Should().Be("2");
        Cell(TimesheetColumnMap.ColRateCount).Should().Be("1");
        // група без годин (Hours1To4 = 0) — порожньо, не "0"
        Cell(TimesheetColumnMap.ColTariff1To4).Should().BeEmpty();
        // колонки вводу лишаються порожні — їх вписує завуч
        Cell(TimesheetColumnMap.ColWorkedDays).Should().BeEmpty();
    }

    /// <summary>
    /// Працівник без блоку Workload (напр. прибиральниця) → сірі колонки навантаження порожні,
    /// але ставки все одно показуються.
    /// </summary>
    [Fact]
    public async Task No_workload_leaves_gray_columns_empty()
    {
        await SeedEmployeeWithPositionAsync(withWorkload: false);
        await using var db = _fx.CreateContext();
        var service = new TimesheetTemplateService(db, new TemplateGenerator());

        var bytes = await service.BuildAsync(2026, 3);

        var map = new TimesheetColumnMap();
        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var ws = wb.Worksheets.First();
        var dataRow = map.FirstDataRowIndex + 1;

        ws.Cell(dataRow, TimesheetColumnMap.ColTariff5To9 + 1).GetString().Should().BeEmpty();
        ws.Cell(dataRow, TimesheetColumnMap.ColRateCount + 1).GetString().Should().Be("1");
    }

    // ─── helpers ───

    private Task SeedTeacherWithLoadAsync() => SeedEmployeeWithPositionAsync(withWorkload: true);

    private async Task SeedEmployeeWithPositionAsync(bool withWorkload)
    {
        await using var db = _fx.CreateContext();
        var position = await db.Positions.FirstAsync();
        var grade = await db.TariffGrades.FirstAsync();
        var emp = new Employee
        {
            TabNumber = "T001",
            FullName = "Тестовий Вчитель",
            TaxId = Tax,
            HireDate = new DateOnly(2020, 9, 1),
            Status = EmployeeStatus.Active,
        };
        var ep = new EmployeePosition
        {
            Employee = emp,
            PositionId = position.Id,
            TariffGradeId = grade.Id,
            RateCount = 1m,
            IsPrimary = true,
            HireDate = new DateOnly(2020, 9, 1),
            EffectiveFrom = new DateOnly(2020, 9, 1),
        };
        if (withWorkload)
            ep.Workload = new EmployeeWorkload
            {
                Hours5To9 = 18m,
                Hours10To11 = 6m,
                IndividualHours1To4 = 2m,
            };
        db.EmployeePositions.Add(ep);
        await db.SaveChangesAsync();
    }
}
