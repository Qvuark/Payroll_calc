using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.API.Application.Import;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Staff;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Integration-тести StaffImporter: реальний Postgres у Docker через Testcontainers.
/// Один контейнер на весь клас (IClassFixture). Дані в Employees/EmployeePositions
/// чистяться перед кожним тестом, довідники сіються один раз у fixture.
/// </summary>
public class StaffImporterTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fx;

    public StaffImporterTests(PostgresFixture fx) => _fx = fx;

    /// <summary>
    /// xUnit викликає перед кожним [Fact]. Чистимо employee-дані щоб тести були незалежні.
    /// </summary>
    public Task InitializeAsync() => _fx.ResetEmployeeDataAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Шаблонний кейс: 1 валідна строка → 1 Employee + 1 EmployeePosition створено, помилок нема.
    /// </summary>
    [Fact]
    public async Task Imports_single_new_employee_with_one_position()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        await using var xlsx = StaffXlsxBuilder.Build(StaffXlsxBuilder.ValidRow());

        // Act
        var report = await importer.ImportAsync(xlsx);

        // Assert report
        report.Created.Should().Be(1);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(0);
        report.Errors.Should().BeEmpty();

        // Assert DB state — окремий context щоб не читати з change tracker'а.
        await using var readDb = _fx.CreateContext();
        var employee = await readDb.Employees
            .Include(e => e.Positions)
                .ThenInclude(p => p.Position)
            .SingleAsync();
        employee.TaxId.Should().Be("9876543210");
        employee.FullName.Should().Be("Сидоренко Анна Іванівна");
        employee.Positions.Should().HaveCount(1);
        employee.Positions.Single().Position!.Name.Should().Be("Бухгалтер");
        employee.Positions.Single().RateCount.Should().Be(1.0m);
    }

    /// <summary>
    /// Одна людина, 3 рядки = 3 різні посади. У БД: 1 Employee + 3 EmployeePosition.
    /// Перевіряє group-by-TaxId — Employee upsert 1 раз, Position upsert 3 рази.
    /// </summary>
    [Fact]
    public async Task Imports_one_employee_with_three_positions()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row1 = StaffXlsxBuilder.ValidRow();
        row1[StaffColumnMap.ColPosition] = "Бухгалтер";
        row1[StaffColumnMap.ColTariffGrade] = 12;
        row1[StaffColumnMap.ColIsPrimary] = true;

        var row2 = StaffXlsxBuilder.ValidRow();
        row2[StaffColumnMap.ColPosition] = "Прибиральник службових приміщень";
        row2[StaffColumnMap.ColTariffGrade] = 2;

        var row3 = StaffXlsxBuilder.ValidRow();
        row3[StaffColumnMap.ColPosition] = "Сторож";
        row3[StaffColumnMap.ColTariffGrade] = 2;

        await using var xlsx = StaffXlsxBuilder.Build(row1, row2, row3);

        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(3);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(0);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var employees = await readDb.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .ToListAsync();
        employees.Should().HaveCount(1);
        employees[0].Positions.Should().HaveCount(3);
        employees[0].Positions.Select(p => p.Position!.Name)
            .Should().BeEquivalentTo(["Бухгалтер", "Прибиральник службових приміщень", "Сторож"]);
    }

    /// <summary>
    /// Pre-seed Employee + EmployeePosition. Імпорт того ж TaxId+Position з новими даними:
    /// FullName змінився, RateCount 0.5 → 1.0. Очікуємо Updated=1, не створено новий запис.
    /// </summary>
    [Fact]
    public async Task Updates_existing_employee_and_position()
    {
        // ─── Pre-seed: людина + позиція у БД до імпорту ───
        await using (var seedDb = _fx.CreateContext())
        {
            var pos = await seedDb.Positions.SingleAsync(p => p.Name == "Бухгалтер");
            var grade = await seedDb.TariffGrades.SingleAsync(t => t.Grade == 10);
            var existing = new Employee
            {
                TabNumber = "OLD001",
                FullName = "Стара Назва",
                TaxId = "9876543210",
                HireDate = new DateOnly(2010, 1, 1),
                Status = EmployeeStatus.Active,
            };
            existing.Positions.Add(new EmployeePosition
            {
                Position = pos,
                TariffGrade = grade,
                RateCount = 0.5m,
                HireDate = new DateOnly(2010, 1, 1),
                EffectiveFrom = new DateOnly(2010, 1, 1),
            });
            seedDb.Employees.Add(existing);
            await seedDb.SaveChangesAsync();
        }

        // ─── Act: імпорт того ж TaxId з новими полями ───
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        // ValidRow = TaxId 9876543210, FullName "Сидоренко...", Position "Бухгалтер", Stavki 1.0
        await using var xlsx = StaffXlsxBuilder.Build(StaffXlsxBuilder.ValidRow());

        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(0);
        report.Updated.Should().Be(1);
        report.Skipped.Should().Be(0);
        report.Errors.Should().BeEmpty();

        // ─── Assert: той самий рядок оновлений, новий не створено ───
        await using var readDb = _fx.CreateContext();
        var employees = await readDb.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .ToListAsync();
        employees.Should().HaveCount(1);
        employees[0].FullName.Should().Be("Сидоренко Анна Іванівна");
        employees[0].Positions.Should().HaveCount(1);
        employees[0].Positions.Single().RateCount.Should().Be(1.0m);
    }

    /// <summary>
    /// Резолв посади впав (немає "Кочегар" у довіднику). Importer пропускає рядок
    /// + додає помилку у звіт. Orphan guard у Importer'і відкочує щойно створеного Employee
    /// бо у нього 0 успішних позицій — у БД пусто.
    /// </summary>
    [Fact]
    public async Task Skips_row_and_drops_orphan_employee_when_only_position_fails()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = StaffXlsxBuilder.ValidRow();
        row[StaffColumnMap.ColPosition] = "Кочегар";
        await using var xlsx = StaffXlsxBuilder.Build(row);

        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(0);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(1);
        report.Errors.Should().HaveCount(1);
        report.Errors[0].Field.Should().Be("Position");
        report.Errors[0].Message.Should().Contain("Кочегар");

        // Orphan guard: Employee нема в БД бо жодна його позиція не вдалася.
        await using var readDb = _fx.CreateContext();
        var employees = await readDb.Employees.ToListAsync();
        employees.Should().BeEmpty();
        var positions = await readDb.EmployeePositions.ToListAsync();
        positions.Should().BeEmpty();
    }

    /// <summary>
    /// Одна людина, 2 рядки: 1 валідна посада + 1 невалідна. Employee живе бо хоч одна
    /// позиція вдалась. Перевіряє що orphan guard НЕ хибно зрубає Employee якщо є успіх.
    /// </summary>
    [Fact]
    public async Task Keeps_employee_when_at_least_one_position_succeeds()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var validRow = StaffXlsxBuilder.ValidRow();
        validRow[StaffColumnMap.ColPosition] = "Бухгалтер";

        var invalidRow = StaffXlsxBuilder.ValidRow();
        invalidRow[StaffColumnMap.ColPosition] = "Кочегар";

        await using var xlsx = StaffXlsxBuilder.Build(validRow, invalidRow);

        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(1);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(1);
        report.Errors.Should().HaveCount(1);
        report.Errors[0].Message.Should().Contain("Кочегар");

        await using var readDb = _fx.CreateContext();
        var employees = await readDb.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .ToListAsync();
        employees.Should().HaveCount(1);
        employees[0].Positions.Should().HaveCount(1);
        employees[0].Positions.Single().Position!.Name.Should().Be("Бухгалтер");
    }

    /// <summary>
    /// Невалідний TaxId (5 цифр замість 10) — парсер відкидає рядок ще до Importer'а.
    /// rows.Count = 0, лічильники нульові, помилка парсера присутня у звіті.
    /// </summary>
    [Fact]
    public async Task Propagates_parser_errors_for_invalid_tax_id()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = StaffXlsxBuilder.ValidRow();
        row[StaffColumnMap.ColTaxId] = "12345";
        await using var xlsx = StaffXlsxBuilder.Build(row);

        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(0);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(0);
        report.Errors.Should().NotBeEmpty();
        report.Errors.Should().Contain(e => e.Field == "TaxId" && e.Message.Contains("ІПН"));

        await using var readDb = _fx.CreateContext();
        var employees = await readDb.Employees.ToListAsync();
        employees.Should().BeEmpty();
    }

    /// <summary>
    /// Маленький DRY-хелпер: збирає Importer з його залежностями на переданий DbContext.
    /// </summary>
    private static StaffImporter BuildImporter(Infrastructure.Data.AppDbContext db) =>
        new(new StaffParser(), new EmployeeUpserter(db), new PositionUpserter(db), db);
}
