using FluentAssertions;
using PayrollCalc.API.Application.AvgSalary;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Integration-тести AvgBaseAutoFiller: авто-режим заповнює base-поля події з підписаної історії,
/// fallback лишає ручні поля при недостатній історії, у Manual не чіпає нічого.
/// </summary>
public class AvgBaseAutoFillerTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fx;

    public AvgBaseAutoFillerTests(PostgresFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetEmployeeDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Auto + 12 підписаних місяців: база й виключені дні заповнюються з історії.
    /// </summary>
    [Fact]
    public async Task FillSick_auto_fills_base_and_excluded_when_history_full()
    {
        var emp = await SeedEmployeeAsync();
        await SeedSignedMonthsAsync(emp, 12, 1000m, 2026, 3);
        await using (var seed = _fx.CreateContext())
        {
            seed.SickLeaves.Add(new SickLeave { EmployeeId = emp, StartDate = new DateOnly(2025, 7, 1), DaysTotal = 5 });
            seed.Vacations.Add(new Vacation { EmployeeId = emp, VacationType = VacationType.Unpaid, StartDate = new DateOnly(2025, 8, 1), CalendarDays = 10 });
            await seed.SaveChangesAsync();
        }
        await using var db = _fx.CreateContext();
        var e = new SickLeave { EmployeeId = emp, BaseCalculationMode = CalcMode.Auto, StartDate = new DateOnly(2026, 3, 5), DaysTotal = 10 };

        await new AvgBaseAutoFiller(new AvgBaseBuilder(db)).FillSickAsync(e);

        e.BaseAmount.Should().Be(12000m);    // 12 міс × 1000
        e.BaseExcludedDays.Should().Be(15);  // 5 лікарняних + 10 за свій рахунок
    }

    /// <summary>
    /// Auto, але історії мало (11 міс): поля лишаються ручними (fallback, не перетираємо).
    /// </summary>
    [Fact]
    public async Task FillSick_keeps_manual_when_history_insufficient()
    {
        var emp = await SeedEmployeeAsync();
        await SeedSignedMonthsAsync(emp, 11, 1000m, 2026, 3);
        await using var db = _fx.CreateContext();
        var e = new SickLeave { EmployeeId = emp, BaseCalculationMode = CalcMode.Auto, StartDate = new DateOnly(2026, 3, 5), DaysTotal = 10, BaseAmount = 999m };

        await new AvgBaseAutoFiller(new AvgBaseBuilder(db)).FillSickAsync(e);

        e.BaseAmount.Should().Be(999m);     // не чіпали — історії менше 12 міс
        e.BaseExcludedDays.Should().Be(0);
    }

    /// <summary>
    /// Manual: filler виходить одразу, навіть якщо історія повна.
    /// </summary>
    [Fact]
    public async Task FillSick_noop_when_manual()
    {
        var emp = await SeedEmployeeAsync();
        await SeedSignedMonthsAsync(emp, 12, 1000m, 2026, 3);
        await using var db = _fx.CreateContext();
        var e = new SickLeave { EmployeeId = emp, BaseCalculationMode = CalcMode.Manual, StartDate = new DateOnly(2026, 3, 5), DaysTotal = 10, BaseAmount = 999m };

        await new AvgBaseAutoFiller(new AvgBaseBuilder(db)).FillSickAsync(e);

        e.BaseAmount.Should().Be(999m);     // Manual — нічого не рахуємо
    }

    /// <summary>
    /// Курси, Auto: база за 2 міс + робочі дні з work_calendar.
    /// </summary>
    [Fact]
    public async Task FillTraining_auto_fills_from_two_months()
    {
        var emp = await SeedEmployeeAsync();
        await using (var seed = _fx.CreateContext())
        {
            seed.Calculations.AddRange(
                Month(emp, 2099, 11, CalculationStatus.Signed, 1000m),
                Month(emp, 2099, 12, CalculationStatus.Signed, 1000m));
            seed.WorkCalendars.Add(new WorkCalendar { Year = 2099, Month = 11, WorkDays = 21 });
            seed.WorkCalendars.Add(new WorkCalendar { Year = 2099, Month = 12, WorkDays = 22 });
            await seed.SaveChangesAsync();
        }
        await using var db = _fx.CreateContext();
        var e = new TrainingLeave { EmployeeId = emp, BaseCalculationMode = CalcMode.Auto, StartDate = new DateOnly(2100, 1, 5), WorkingDaysAbsent = 3 };

        await new AvgBaseAutoFiller(new AvgBaseBuilder(db)).FillTrainingAsync(e);

        e.BaseAmount.Should().Be(2000m);     // 2 міс × 1000
        e.BaseWorkingDays.Should().Be(43);   // 21 + 22 із work_calendar
    }

    // ─── helpers ───

    private async Task<int> SeedEmployeeAsync()
    {
        await using var db = _fx.CreateContext();
        var emp = new Employee
        {
            TabNumber = "T100",
            FullName = "Тестовий Іван",
            TaxId = "5555555555",
            HireDate = new DateOnly(2020, 1, 1),
            Status = EmployeeStatus.Active,
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        return emp.Id;
    }

    /// <summary>
    /// Сіє count підписаних місяців перед подією (eventYear, eventMonth), у кожному — оклад salaryEach.
    /// </summary>
    private async Task SeedSignedMonthsAsync(int emp, int count, decimal salaryEach, int eventYear, int eventMonth)
    {
        await using var db = _fx.CreateContext();
        var lastMonth = new DateOnly(eventYear, eventMonth, 1).AddMonths(-1);
        for (var i = 0; i < count; i++)
        {
            var d = lastMonth.AddMonths(-i);
            db.Calculations.Add(Month(emp, d.Year, d.Month, CalculationStatus.Signed, salaryEach));
        }
        await db.SaveChangesAsync();
    }

    private static PayrollCalc.Core.Entities.Calculation Month(int emp, int year, int month, CalculationStatus status, decimal salary)
        => new()
        {
            EmployeeId = emp,
            Year = year,
            Month = month,
            Status = status,
            Components = [new CalculationComponent { Kind = ComponentKind.Earning, FieldKey = "Оклад", Amount = salary }],
        };
}
