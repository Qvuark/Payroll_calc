using FluentAssertions;
using PayrollCalc.API.Application.AvgSalary;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Integration-тести AvgBaseBuilder: реальний Postgres із засіяним довідником inclusion_rules.
/// Перевіряють серце авто-середньоденної — Σ підписаних нарахувань за вікно по FieldKey ↔ довідник:
/// удержання й чернетки відсікаються, вид події перемикає колонку галок, вікно різної довжини.
/// </summary>
public class AvgBaseBuilderTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fx;

    public AvgBaseBuilderTests(PostgresFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetEmployeeDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Лікарняний: сумуються лише нарахування підписаних місяців у 12-міс вікні. Удержання
    /// (ПДФО), чернетка й місяць поза вікном відсікаються; monthsFound = к-сть підписаних місяців.
    /// </summary>
    [Fact]
    public async Task Sick_sums_signed_earnings_in_window_and_counts_months()
    {
        var emp = await SeedEmployeeAsync();
        await using (var seed = _fx.CreateContext())
        {
            seed.Calculations.AddRange(
                Month(emp, 2026, 2, CalculationStatus.Signed,
                    ("Оклад", ComponentKind.Earning, 7000m),
                    ("Премія", ComponentKind.Earning, 1000m),
                    ("ПДФО", ComponentKind.Deduction, -1400m)),
                Month(emp, 2026, 1, CalculationStatus.Signed,
                    ("Оклад", ComponentKind.Earning, 7000m)),
                Month(emp, 2025, 12, CalculationStatus.Draft,
                    ("Оклад", ComponentKind.Earning, 7000m)),
                Month(emp, 2025, 2, CalculationStatus.Signed,
                    ("Оклад", ComponentKind.Earning, 7000m)));
            await seed.SaveChangesAsync();
        }
        await using var db = _fx.CreateContext();

        var result = await new AvgBaseBuilder(db).BuildAmountAsync(emp, 2026, 3, AvgBaseKind.Sick);

        // 7000+1000 (лютий, ПДФО геть) + 7000 (січень). Грудень — Draft, лютий-2025 — поза вікном.
        result.Amount.Should().Be(15000m);
        result.SignedMonths.Should().Be(2);
    }

    /// <summary>
    /// Той самий набір, але вид Vacation: у Премії IncludeVacation=false → вона випадає,
    /// сума менша. Доводить, що switch бере правильну колонку галок під вид події.
    /// </summary>
    [Fact]
    public async Task Vacation_excludes_premia_via_kind_column()
    {
        var emp = await SeedEmployeeAsync();
        await using (var seed = _fx.CreateContext())
        {
            seed.Calculations.AddRange(
                Month(emp, 2026, 2, CalculationStatus.Signed,
                    ("Оклад", ComponentKind.Earning, 7000m),
                    ("Премія", ComponentKind.Earning, 1000m)),
                Month(emp, 2026, 1, CalculationStatus.Signed,
                    ("Оклад", ComponentKind.Earning, 7000m)));
            await seed.SaveChangesAsync();
        }
        await using var db = _fx.CreateContext();

        var result = await new AvgBaseBuilder(db).BuildAmountAsync(emp, 2026, 3, AvgBaseKind.Vacation);

        // 7000 + 7000; Премія не входить у відпускну базу (IncludeVacation=false).
        result.Amount.Should().Be(14000m);
        result.SignedMonths.Should().Be(2);
    }

    /// <summary>
    /// Немає історії → SUM по нулю рядків дає NULL, який ?? 0m робить нулем (не падіння).
    /// </summary>
    [Fact]
    public async Task No_history_returns_zero()
    {
        var emp = await SeedEmployeeAsync();
        await using var db = _fx.CreateContext();

        var result = await new AvgBaseBuilder(db).BuildAmountAsync(emp, 2026, 3, AvgBaseKind.Sick);

        result.Amount.Should().Be(0m);
        result.SignedMonths.Should().Be(0);
    }

    /// <summary>
    /// Довжина вікна залежить від виду: курси = 2 міс (грудень випадає), лікарняний = 12 міс
    /// (грудень входить). Один набір даних, два виклики — різні суми й monthsFound.
    /// </summary>
    [Fact]
    public async Task Training_window_is_two_months_unlike_sick()
    {
        var emp = await SeedEmployeeAsync();
        await using (var seed = _fx.CreateContext())
        {
            seed.Calculations.AddRange(
                Month(emp, 2025, 12, CalculationStatus.Signed, ("Оклад", ComponentKind.Earning, 1000m)),
                Month(emp, 2026, 1, CalculationStatus.Signed, ("Оклад", ComponentKind.Earning, 1000m)),
                Month(emp, 2026, 2, CalculationStatus.Signed, ("Оклад", ComponentKind.Earning, 1000m)));
            await seed.SaveChangesAsync();
        }
        await using var db = _fx.CreateContext();
        var builder = new AvgBaseBuilder(db);

        var training = await builder.BuildAmountAsync(emp, 2026, 3, AvgBaseKind.Training);
        var sick = await builder.BuildAmountAsync(emp, 2026, 3, AvgBaseKind.Sick);

        // Курси: вікно січень..лютий → 2 місяці. Грудень за межами.
        training.Amount.Should().Be(2000m);
        training.SignedMonths.Should().Be(2);
        // Лікарняний: вікно 12 міс → грудень теж усередині.
        sick.Amount.Should().Be(3000m);
        sick.SignedMonths.Should().Be(3);
    }

    /// <summary>
    /// Виключені дні лікарняного = минулі лікарняні + відпустки за свій рахунок у вікні.
    /// Оплачувана відпустка й події поза вікном не рахуються.
    /// </summary>
    [Fact]
    public async Task Sick_excluded_days_sum_past_sick_and_unpaid()
    {
        var emp = await SeedEmployeeAsync();
        await using (var seed = _fx.CreateContext())
        {
            seed.SickLeaves.Add(new SickLeave { EmployeeId = emp, StartDate = new DateOnly(2025, 6, 10), DaysTotal = 20 });
            seed.SickLeaves.Add(new SickLeave { EmployeeId = emp, StartDate = new DateOnly(2024, 1, 1), DaysTotal = 99 }); // поза вікном
            seed.Vacations.Add(new Vacation { EmployeeId = emp, VacationType = VacationType.Unpaid, StartDate = new DateOnly(2025, 8, 1), CalendarDays = 14 });
            seed.Vacations.Add(new Vacation { EmployeeId = emp, VacationType = VacationType.Annual, StartDate = new DateOnly(2025, 9, 1), CalendarDays = 10 }); // оплачувана — не рахується
            await seed.SaveChangesAsync();
        }
        await using var db = _fx.CreateContext();

        var excluded = await new AvgBaseBuilder(db).BuildExcludedDaysAsync(emp, 2026, 3, AvgBaseKind.Sick);

        excluded.Should().Be(34); // 20 лікарняних + 14 за свій рахунок
    }

    /// <summary>
    /// Виключені дні відпускних = лише відпустки за свій рахунок (минулі лікарняні НЕ віднімаються,
    /// держсвята призупинені воєнним станом). Той самий набір даних, інший вид — інше число.
    /// </summary>
    [Fact]
    public async Task Vacation_excluded_days_sum_unpaid_only()
    {
        var emp = await SeedEmployeeAsync();
        await using (var seed = _fx.CreateContext())
        {
            seed.SickLeaves.Add(new SickLeave { EmployeeId = emp, StartDate = new DateOnly(2025, 6, 10), DaysTotal = 20 });
            seed.Vacations.Add(new Vacation { EmployeeId = emp, VacationType = VacationType.Unpaid, StartDate = new DateOnly(2025, 8, 1), CalendarDays = 14 });
            await seed.SaveChangesAsync();
        }
        await using var db = _fx.CreateContext();

        var excluded = await new AvgBaseBuilder(db).BuildExcludedDaysAsync(emp, 2026, 3, AvgBaseKind.Vacation);

        excluded.Should().Be(14); // лише за свій рахунок; 20 лікарняних не входять у базу відпускних
    }

    /// <summary>
    /// Знаменник курсів = сума WorkDays двох місяців перед подією з work_calendar.
    /// </summary>
    [Fact]
    public async Task Training_working_days_sum_two_months_from_calendar()
    {
        await using (var seed = _fx.CreateContext())
        {
            seed.WorkCalendars.Add(new WorkCalendar { Year = 2099, Month = 11, WorkDays = 21 });
            seed.WorkCalendars.Add(new WorkCalendar { Year = 2099, Month = 12, WorkDays = 22 });
            await seed.SaveChangesAsync();
        }
        await using var db = _fx.CreateContext();

        var days = await new AvgBaseBuilder(db).BuildTrainingWorkingDaysAsync(2100, 1);

        days.Should().Be(43); // листопад 21 + грудень 22
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

    private static PayrollCalc.Core.Entities.Calculation Month(int empId, int year, int month, CalculationStatus status,
        params (string name, ComponentKind kind, decimal amount)[] comps)
        => new()
        {
            EmployeeId = empId,
            Year = year,
            Month = month,
            Status = status,
            Components =
                [.. comps.Select(c => new CalculationComponent { Kind = c.kind, FieldKey = c.name, Amount = c.amount })],
        };
}
