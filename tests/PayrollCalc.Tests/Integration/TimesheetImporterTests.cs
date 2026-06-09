using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.API.Application.Import;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Timesheet;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Integration-тести TimesheetImporter: реальний Postgres у Docker. Importer матчить по TaxId
/// (людину НЕ створює) + потребує WorkCalendar на період (сіється у fixture: 2026 усі місяці,
/// березень = 21 день).
/// </summary>
public class TimesheetImporterTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fx;
    private const string Tax = "9876543210";

    public TimesheetImporterTests(PostgresFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetEmployeeDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Існуючий працівник + валідний рядок → 1 Timesheet створено, значення на місці.
    /// </summary>
    [Fact]
    public async Task Imports_new_timesheet_for_existing_employee()
    {
        var empId = await SeedEmployeeAsync();
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        await using var xlsx = TimesheetXlsxBuilder.Build(TimesheetXlsxBuilder.ValidRow());

        var report = await importer.ImportAsync(xlsx, 2026, 3);

        report.Created.Should().Be(1);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(0);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var ts = await readDb.Timesheets.SingleAsync();
        ts.EmployeeId.Should().Be(empId);
        ts.Year.Should().Be(2026);
        ts.Month.Should().Be(3);
        ts.WorkedDays.Should().Be(20m);
        ts.ReplacementHours.Should().Be(5m);
        ts.NightHours.Should().Be(8m);
    }

    /// <summary>
    /// Pre-seed Timesheet (wd=10). Імпорт того ж (Employee, період) → Updated=1, рядок один (не дубль).
    /// </summary>
    [Fact]
    public async Task Updates_existing_timesheet_without_duplicating()
    {
        var empId = await SeedEmployeeAsync();
        await SeedTimesheetAsync(empId, wd: 10m);

        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        await using var xlsx = TimesheetXlsxBuilder.Build(TimesheetXlsxBuilder.ValidRow());

        var report = await importer.ImportAsync(xlsx, 2026, 3);

        report.Created.Should().Be(0);
        report.Updated.Should().Be(1);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var sheets = await readDb.Timesheets.ToListAsync();
        sheets.Should().ContainSingle();
        sheets[0].WorkedDays.Should().Be(20m);
    }

    /// <summary>
    /// КЛЮЧОВИЙ: pre-seed Timesheet з Advance=500 + AnnualBonus=300 (гроші з CRUD).
    /// Імпорт пише лише 3 поля вводу → гроші лишаються незмінні.
    /// </summary>
    [Fact]
    public async Task Import_preserves_crud_money_fields()
    {
        var empId = await SeedEmployeeAsync();
        await SeedTimesheetAsync(empId, wd: 10m, advance: 500m, annualBonus: 300m);

        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        await using var xlsx = TimesheetXlsxBuilder.Build(TimesheetXlsxBuilder.ValidRow());

        var report = await importer.ImportAsync(xlsx, 2026, 3);

        report.Updated.Should().Be(1);

        await using var readDb = _fx.CreateContext();
        var ts = await readDb.Timesheets.SingleAsync();
        ts.WorkedDays.Should().Be(20m);        // ввід оновлено
        ts.ReplacementHours.Should().Be(5m);
        ts.NightHours.Should().Be(8m);
        ts.Advance.Should().Be(500m);          // гроші з CRUD НЕ чіпані
        ts.AnnualBonus.Should().Be(300m);
    }

    /// <summary>
    /// ІПН з файлу немає в БД → рядок у Errors, людину не створюємо, Timesheet не пишемо.
    /// </summary>
    [Fact]
    public async Task Employee_not_found_row_error_and_no_create()
    {
        await SeedEmployeeAsync(taxId: "1111111111");  // інший TaxId — 9876543210 не існує
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        await using var xlsx = TimesheetXlsxBuilder.Build(TimesheetXlsxBuilder.ValidRow());

        var report = await importer.ImportAsync(xlsx, 2026, 3);

        report.Created.Should().Be(0);
        report.Skipped.Should().Be(1);
        report.Errors.Should().ContainSingle();
        report.Errors[0].Field.Should().Be("TaxId");
        report.Errors[0].Message.Should().Contain("не знайдено");

        await using var readDb = _fx.CreateContext();
        (await readDb.Timesheets.ToListAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Відпрацьовано днів більше норми місяця → рядок у Errors, skip, нічого не пишемо.
    /// </summary>
    [Fact]
    public async Task WorkedDays_over_norm_row_error_skipped()
    {
        await SeedEmployeeAsync();
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        var row = TimesheetXlsxBuilder.ValidRow();
        row[TimesheetColumnMap.ColWorkedDays] = 25.0;  // норма 2026-03 = 21
        await using var xlsx = TimesheetXlsxBuilder.Build(row);

        var report = await importer.ImportAsync(xlsx, 2026, 3);

        report.Created.Should().Be(0);
        report.Skipped.Should().Be(1);
        report.Errors.Should().ContainSingle();
        report.Errors[0].Field.Should().Be("WorkedDays");

        await using var readDb = _fx.CreateContext();
        (await readDb.Timesheets.ToListAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Нема WorkCalendar на період → import-level помилка, 0 рядків оброблено.
    /// </summary>
    [Fact]
    public async Task No_calendar_for_period_import_level_error()
    {
        await SeedEmployeeAsync();
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        await using var xlsx = TimesheetXlsxBuilder.Build(TimesheetXlsxBuilder.ValidRow());

        // 2025 немає у WorkCalendar (сіється лише 2026)
        var report = await importer.ImportAsync(xlsx, 2025, 3);

        report.Created.Should().Be(0);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(0);
        report.Errors.Should().ContainSingle();
        report.Errors[0].Message.Should().Contain("календаря");

        await using var readDb = _fx.CreateContext();
        (await readDb.Timesheets.ToListAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Дубль ІПН у файлі: перший рядок виграє, другий → skip + Warning (захист від unique-violation).
    /// </summary>
    [Fact]
    public async Task Duplicate_taxid_in_file_second_skipped_with_warning()
    {
        await SeedEmployeeAsync();
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row1 = TimesheetXlsxBuilder.ValidRow();      // wd 20
        var row2 = TimesheetXlsxBuilder.ValidRow();
        row2[TimesheetColumnMap.ColWorkedDays] = 15.0;   // той самий TaxId
        await using var xlsx = TimesheetXlsxBuilder.Build(row1, row2);

        var report = await importer.ImportAsync(xlsx, 2026, 3);

        report.Created.Should().Be(1);
        report.Skipped.Should().Be(1);
        report.Errors.Should().ContainSingle();
        report.Errors[0].Severity.Should().Be(ErrorSeverity.Warning);
        report.Errors[0].Message.Should().Contain("Дубль");

        await using var readDb = _fx.CreateContext();
        var ts = await readDb.Timesheets.SingleAsync();
        ts.WorkedDays.Should().Be(20m);   // перша строка виграла
    }

    /// <summary>
    /// Невалідний TaxId (3 цифри) — парсер відкидає рядок до Importer'а. Лічильники нульові,
    /// помилка парсера у звіті, у БД пусто.
    /// </summary>
    [Fact]
    public async Task Invalid_taxid_parser_error_propagated()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        var row = TimesheetXlsxBuilder.ValidRow();
        row[TimesheetColumnMap.ColTaxId] = "123";
        await using var xlsx = TimesheetXlsxBuilder.Build(row);

        var report = await importer.ImportAsync(xlsx, 2026, 3);

        report.Created.Should().Be(0);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(0);
        report.Errors.Should().Contain(e => e.Field == "TaxId" && e.Message.Contains("ІПН"));

        await using var readDb = _fx.CreateContext();
        (await readDb.Timesheets.ToListAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Повторний імпорт того ж файлу → run1 Created, run2 Updated, рядок у БД один (unique-індекс).
    /// </summary>
    [Fact]
    public async Task Reimport_is_idempotent_no_duplicate_rows()
    {
        await SeedEmployeeAsync();

        await using (var db1 = _fx.CreateContext())
        await using (var xlsx1 = TimesheetXlsxBuilder.Build(TimesheetXlsxBuilder.ValidRow()))
            (await BuildImporter(db1).ImportAsync(xlsx1, 2026, 3)).Created.Should().Be(1);

        ImportReport report2;
        await using (var db2 = _fx.CreateContext())
        await using (var xlsx2 = TimesheetXlsxBuilder.Build(TimesheetXlsxBuilder.ValidRow()))
            report2 = await BuildImporter(db2).ImportAsync(xlsx2, 2026, 3);

        report2.Created.Should().Be(0);
        report2.Updated.Should().Be(1);

        await using var readDb = _fx.CreateContext();
        (await readDb.Timesheets.ToListAsync()).Should().ContainSingle();
    }

    /// <summary>
    /// Від'ємні дні/години у файлі → рядок у Errors, skip. CRUD ріже Range-атрибутами,
    /// імпортний шлях мусить різати сам — інакше у відомості з'явився б від'ємний оклад.
    /// </summary>
    [Fact]
    public async Task Negative_values_row_error_skipped()
    {
        await SeedEmployeeAsync();
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        var row = TimesheetXlsxBuilder.ValidRow();
        row[TimesheetColumnMap.ColWorkedDays] = -5.0;
        await using var xlsx = TimesheetXlsxBuilder.Build(row);

        var report = await importer.ImportAsync(xlsx, 2026, 3);

        report.Created.Should().Be(0);
        report.Skipped.Should().Be(1);
        report.Errors.Should().ContainSingle();
        report.Errors[0].Message.Should().Contain("Від'ємні");

        await using var readDb = _fx.CreateContext();
        (await readDb.Timesheets.ToListAsync()).Should().BeEmpty();
    }

    // ─── helpers ───

    private async Task<int> SeedEmployeeAsync(string taxId = Tax)
    {
        await using var db = _fx.CreateContext();
        var emp = new Employee
        {
            TabNumber = "T001",
            FullName = "Тестовий Працівник",
            TaxId = taxId,
            HireDate = new DateOnly(2020, 9, 1),
            Status = EmployeeStatus.Active,
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        return emp.Id;
    }

    private async Task SeedTimesheetAsync(int employeeId, decimal wd, decimal advance = 0m, decimal annualBonus = 0m)
    {
        await using var db = _fx.CreateContext();
        db.Timesheets.Add(new Timesheet
        {
            EmployeeId = employeeId,
            Year = 2026,
            Month = 3,
            WorkedDays = wd,
            Advance = advance,
            AnnualBonus = annualBonus,
        });
        await db.SaveChangesAsync();
    }

    private static TimesheetImporter BuildImporter(AppDbContext db) =>
        new(new TimesheetParser(), new TimesheetUpserter(db), db);
}
