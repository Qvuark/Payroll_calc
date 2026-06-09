using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.API.Application.Calculation;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Integration-тести CalcInputBuilder: реальний Postgres, повна проводка БД → вхід рушія.
/// Ловлять головний клас регресій — поле є в БД і в рушії, але білдер його не переписує
/// (рушій тоді мовчки рахує без надбавки, diff-тести рушія цього не бачать).
/// </summary>
public class CalcInputBuilderTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fx;

    public CalcInputBuilderTests(PostgresFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetEmployeeDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Вчитель з повним блоком навантаження + табелем: зошити (години+% за предметом),
    /// інклюзив, 2600, заміни — все доходить до PositionCalcInput.
    /// </summary>
    [Fact]
    public async Task Teacher_workload_and_timesheet_fully_wired()
    {
        var empId = await SeedTeacherAsync();
        await using var db = _fx.CreateContext();

        var input = await new CalcInputBuilder(db).BuildAsync(empId, 2026, 3);

        input.Should().NotBeNull();
        input!.NormDays.Should().Be(22);
        input.WorkedDays.Should().Be(20m);

        var pos = input.Positions.Single();
        pos.PedHoursWeekly.Should().Be(9m);
        pos.NotebookHours.Should().Be(8m);
        pos.NotebookPct.Should().Be(0.15m);          // "математика" з довідника NotebookRates
        pos.InclusiveHours.Should().Be(3.5m);
        pos.HasUnfavorable2600.Should().BeTrue();
        pos.ReplacementHours.Should().Be(5m);
        pos.IsHourly.Should().BeFalse();
        pos.NightHours.Should().Be(0m);              // нічні лише на погодинній ставці
    }

    /// <summary>
    /// Вислуга бібліотекаря/медпрацівника визначається за назвою посади з довідника.
    /// </summary>
    [Fact]
    public async Task Medic_and_librarian_tenure_flags_resolved_by_position()
    {
        var empId = await SeedTwoSpecialistsAsync();
        await using var db = _fx.CreateContext();

        var input = await new CalcInputBuilder(db).BuildAsync(empId, 2026, 3);

        var medic = input!.Positions.Single(p => p.PositionName == "Сестра медична");
        medic.HasMedicTenure.Should().BeTrue();
        medic.HasLibrarianTenure.Should().BeFalse();

        var librarian = input.Positions.Single(p => p.PositionName == "Завідувач бібліотеки");
        librarian.HasLibrarianTenure.Should().BeTrue();
        librarian.HasMedicTenure.Should().BeFalse();
    }

    /// <summary>
    /// Сторож: IsHourly з сіда посади вмикає погодинну гілку, години/нічні течуть з табеля.
    /// </summary>
    [Fact]
    public async Task Guard_hourly_branch_wired_from_seeded_position()
    {
        var empId = await SeedGuardAsync();
        await using var db = _fx.CreateContext();

        var input = await new CalcInputBuilder(db).BuildAsync(empId, 2026, 3);

        var pos = input!.Positions.Single();
        pos.IsHourly.Should().BeTrue();
        pos.WorkedHours.Should().Be(187m);
        pos.NightHours.Should().Be(126m);
    }

    // ─── helpers ───

    private async Task<int> SeedTeacherAsync()
    {
        await using var db = _fx.CreateContext();
        var position = await db.Positions.SingleAsync(p => p.Name == "Вчитель");
        var grade = await db.TariffGrades.SingleAsync(t => t.Grade == 14);
        var notebookRate = await db.NotebookRates.SingleAsync(r => r.SubjectKeyword == "математика");
        var emp = NewEmployee("1111111111");
        db.Employees.Add(emp);
        db.EmployeePositions.Add(new EmployeePosition
        {
            Employee = emp,
            PositionId = position.Id,
            TariffGradeId = grade.Id,
            RateCount = 1m,
            IsPrimary = true,
            HireDate = emp.HireDate,
            EffectiveFrom = emp.HireDate,
            HasUnfavorable = true,
            Workload = new EmployeeWorkload
            {
                Hours5To9 = 9m,
                NotebookHours5To9 = 8m,
                InclusiveHours5To9 = 3.5m,
                NotebookRateId = notebookRate.Id,
            },
        });
        db.Timesheets.Add(new Timesheet
        {
            Employee = emp,
            Year = 2026,
            Month = 3,
            WorkedDays = 20m,
            ReplacementHours = 5m,
            NightHours = 8m,
        });
        await db.SaveChangesAsync();
        return emp.Id;
    }

    private async Task<int> SeedTwoSpecialistsAsync()
    {
        await using var db = _fx.CreateContext();
        var medicPos = await db.Positions.SingleAsync(p => p.Name == "Сестра медична");
        var libPos = await db.Positions.SingleAsync(p => p.Name == "Завідувач бібліотеки");
        var grade = await db.TariffGrades.SingleAsync(t => t.Grade == 9);
        var emp = NewEmployee("2222222222");
        db.Employees.Add(emp);
        db.EmployeePositions.AddRange(
            new EmployeePosition
            {
                Employee = emp, PositionId = medicPos.Id, TariffGradeId = grade.Id,
                RateCount = 1m, IsPrimary = true, HireDate = emp.HireDate, EffectiveFrom = emp.HireDate,
            },
            new EmployeePosition
            {
                Employee = emp, PositionId = libPos.Id, TariffGradeId = grade.Id,
                RateCount = 0.5m, HireDate = emp.HireDate, EffectiveFrom = emp.HireDate,
            });
        await db.SaveChangesAsync();
        return emp.Id;
    }

    private async Task<int> SeedGuardAsync()
    {
        await using var db = _fx.CreateContext();
        var position = await db.Positions.SingleAsync(p => p.Name == "Сторож");
        var grade = await db.TariffGrades.SingleAsync(t => t.Grade == 2);
        var emp = NewEmployee("3333333333");
        db.Employees.Add(emp);
        db.EmployeePositions.Add(new EmployeePosition
        {
            Employee = emp, PositionId = position.Id, TariffGradeId = grade.Id,
            RateCount = 1m, IsPrimary = true, HireDate = emp.HireDate, EffectiveFrom = emp.HireDate,
        });
        db.Timesheets.Add(new Timesheet
        {
            Employee = emp,
            Year = 2026,
            Month = 3,
            WorkedDays = 20m,
            WorkedHours = 187m,
            NightHours = 126m,
        });
        await db.SaveChangesAsync();
        return emp.Id;
    }

    private static Employee NewEmployee(string taxId) => new()
    {
        TabNumber = "T" + taxId[..3],
        FullName = "Тестовий Працівник " + taxId[..2],
        TaxId = taxId,
        HireDate = new DateOnly(2020, 9, 1),
        Status = EmployeeStatus.Active,
    };
}
