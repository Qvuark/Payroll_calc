using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.API.Application.Import;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Teachers;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// Integration-тести TeachersImporter: реальний Postgres у Docker через Testcontainers.
/// Дзеркалить StaffImporterTests, але під teachers-схему (Workload + Admin блоки замість Gpd/Pkr/NonPed).
/// </summary>
public class TeachersImporterTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _fx;

    public TeachersImporterTests(PostgresFixture fx) => _fx = fx;

    public Task InitializeAsync() => _fx.ResetEmployeeDataAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Шаблонний кейс: 1 валідна строка → 1 Employee + 1 EmployeePosition створено.
    /// </summary>
    [Fact]
    public async Task Imports_single_new_teacher_with_one_position()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        await using var xlsx = TeachersXlsxBuilder.Build(TeachersXlsxBuilder.ValidRow());

        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(1);
        report.Updated.Should().Be(0);
        report.Skipped.Should().Be(0);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var employee = await readDb.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .SingleAsync();
        employee.TaxId.Should().Be("1234567890");
        employee.FullName.Should().Be("Іваненко Іван Іванович");
        employee.Positions.Should().HaveCount(1);
        employee.Positions.Single().Position!.Name.Should().Be("Вчитель");
    }

    /// <summary>
    /// Director-вчитель: 2 рядки одного TaxId з різними Position (Заступник + Вчитель).
    /// Group by TaxId → 1 Employee + 2 EmployeePosition. Перевіряє multi-position з різними WorkerClass.
    /// </summary>
    [Fact]
    public async Task Imports_one_teacher_with_two_positions()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row1 = TeachersXlsxBuilder.ValidRow();
        row1[TeachersColumnMap.ColPosition] = "Заступник директора з НВР";
        row1[TeachersColumnMap.ColTariffGrade] = 14;
        row1[TeachersColumnMap.ColIsPrimary] = true;

        var row2 = TeachersXlsxBuilder.ValidRow();
        row2[TeachersColumnMap.ColPosition] = "Вчитель";
        row2[TeachersColumnMap.ColTariffGrade] = 12;
        row2[TeachersColumnMap.ColSubject] = "математика";
        row2[TeachersColumnMap.ColHours5To9] = 18.0;

        await using var xlsx = TeachersXlsxBuilder.Build(row1, row2);
        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(2);
        report.Updated.Should().Be(0);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var employees = await readDb.Employees
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .ToListAsync();
        employees.Should().HaveCount(1);
        employees[0].Positions.Should().HaveCount(2);
        employees[0].Positions.Select(p => p.Position!.Name)
            .Should().BeEquivalentTo(["Заступник директора з НВР", "Вчитель"]);
    }

    /// <summary>
    /// Pre-seed Employee + EmployeePosition. Імпорт того ж TaxId+Position з новими даними → Updated=1.
    /// </summary>
    [Fact]
    public async Task Updates_existing_teacher_and_position()
    {
        await using (var seedDb = _fx.CreateContext())
        {
            var pos = await seedDb.Positions.SingleAsync(p => p.Name == "Вчитель");
            var grade = await seedDb.TariffGrades.SingleAsync(t => t.Grade == 10);
            var existing = new Employee
            {
                TabNumber = "OLD",
                FullName = "Стара Назва",
                TaxId = "1234567890",
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

        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);
        await using var xlsx = TeachersXlsxBuilder.Build(TeachersXlsxBuilder.ValidRow());

        var report = await importer.ImportAsync(xlsx);

        report.Updated.Should().Be(1);
        report.Created.Should().Be(0);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var employees = await readDb.Employees.Include(e => e.Positions).ToListAsync();
        employees.Should().HaveCount(1);
        employees[0].FullName.Should().Be("Іваненко Іван Іванович");
        employees[0].Positions.Single().RateCount.Should().Be(1.0m);
    }

    /// <summary>
    /// Невідома посада → ParserError + Skipped=1. Orphan guard зрубає Employee.
    /// </summary>
    [Fact]
    public async Task Skips_row_and_drops_orphan_employee_when_only_position_fails()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = TeachersXlsxBuilder.ValidRow();
        row[TeachersColumnMap.ColPosition] = "Чарівник";
        await using var xlsx = TeachersXlsxBuilder.Build(row);

        var report = await importer.ImportAsync(xlsx);

        report.Skipped.Should().Be(1);
        report.Errors.Should().Contain(e => e.Field == "Position" && e.Message.Contains("Чарівник"));

        await using var readDb = _fx.CreateContext();
        (await readDb.Employees.ToListAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Невалідний TaxId → парсер відкидає рядок до Importer'а. У БД пусто.
    /// </summary>
    [Fact]
    public async Task Propagates_parser_errors_for_invalid_tax_id()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = TeachersXlsxBuilder.ValidRow();
        row[TeachersColumnMap.ColTaxId] = "abc";
        await using var xlsx = TeachersXlsxBuilder.Build(row);

        var report = await importer.ImportAsync(xlsx);

        report.Errors.Should().Contain(e => e.Field == "TaxId");
        (await _fx.CreateContext().Employees.ToListAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Class 1 Вчитель + Workload (Hours5To9, NotebookHours, InclusiveHours) →
    /// EmployeePosition.Workload створено з усіма годинами + NotebookRateId резолвлено по Subject.
    /// </summary>
    [Fact]
    public async Task Imports_workload_block_with_notebook_rate_resolve()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = TeachersXlsxBuilder.ValidRow();
        row[TeachersColumnMap.ColSubject] = "математика";
        row[TeachersColumnMap.ColHours5To9] = 18.0;
        row[TeachersColumnMap.ColNotebookHours5To9] = 18.0;
        row[TeachersColumnMap.ColInclusiveHours5To9] = 2.0;

        await using var xlsx = TeachersXlsxBuilder.Build(row);
        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(1);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var ep = await readDb.EmployeePositions
            .Include(p => p.Workload).ThenInclude(w => w!.NotebookRate)
            .SingleAsync();
        ep.Workload.Should().NotBeNull();
        ep.Workload!.Hours5To9.Should().Be(18.0m);
        ep.Workload.NotebookHours5To9.Should().Be(18.0m);
        ep.Workload.InclusiveHours5To9.Should().Be(2.0m);
        ep.Workload.NotebookRate.Should().NotBeNull();
        ep.Workload.NotebookRate!.SubjectKeyword.Should().Be("математика");
    }

    /// <summary>
    /// Class 1 Вчитель + ClassMgmt "1-4" + CabinetType "звичайний" + Gym=true →
    /// Admin блок з ClassGradeGroup=Grades1To4, CabinetType=Standard, HasGym=true.
    /// </summary>
    [Fact]
    public async Task Imports_admin_block_with_classmgmt_cabinet_and_flags()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = TeachersXlsxBuilder.ValidRow();
        row[TeachersColumnMap.ColClassMgmt] = "1-4";
        row[TeachersColumnMap.ColCabinetType] = "звичайний";
        row[TeachersColumnMap.ColGym] = true;
        row[TeachersColumnMap.ColWebsite] = true;

        await using var xlsx = TeachersXlsxBuilder.Build(row);
        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(1);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var ep = await readDb.EmployeePositions.Include(p => p.Admin).SingleAsync();
        ep.Admin.Should().NotBeNull();
        ep.Admin!.HasClassMgmt.Should().BeTrue();
        ep.Admin.ClassGradeGroup.Should().Be(ClassGradeGroup.Grades1To4);
        ep.Admin.HasCabinet.Should().BeTrue();
        ep.Admin.CabinetType.Should().Be(CabinetType.Standard);
        ep.Admin.HasGym.Should().BeTrue();
        ep.Admin.HasWebsite.Should().BeTrue();
        ep.Admin.HasShootingRange.Should().BeFalse();
    }

    /// <summary>
    /// ClassMgmt "9-10" — невідома група класів. ParserError + Skipped=1, orphan guard зрубає Employee.
    /// </summary>
    [Fact]
    public async Task Rejects_unknown_classmgmt_value()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = TeachersXlsxBuilder.ValidRow();
        row[TeachersColumnMap.ColClassMgmt] = "9-10";

        await using var xlsx = TeachersXlsxBuilder.Build(row);
        var report = await importer.ImportAsync(xlsx);

        report.Skipped.Should().Be(1);
        report.Errors.Should().Contain(e => e.Field == "ClassMgmt" && e.Message.Contains("9-10"));
        (await _fx.CreateContext().Employees.ToListAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// TitleType "Старший вчитель" — резолвиться по (Name, WorkerClass=Pedagogical), записується TitleTypeId.
    /// </summary>
    [Fact]
    public async Task Resolves_title_type_by_worker_class_scope()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = TeachersXlsxBuilder.ValidRow();
        row[TeachersColumnMap.ColTitleType] = "Старший вчитель";

        await using var xlsx = TeachersXlsxBuilder.Build(row);
        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(1);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var emp = await readDb.Employees.Include(e => e.TitleType).SingleAsync();
        emp.TitleType.Should().NotBeNull();
        emp.TitleType!.Name.Should().Be("Старший вчитель");
        emp.TitleType.WorkerClass.Should().Be(WorkerClass.Pedagogical);
    }

    /// <summary>
    /// TitleType "Дед мороз" не в довіднику — ParserError у звіт, але працівник зберігається без звання.
    /// </summary>
    [Fact]
    public async Task Logs_error_but_keeps_employee_for_unknown_title_type()
    {
        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = TeachersXlsxBuilder.ValidRow();
        row[TeachersColumnMap.ColTitleType] = "Дед мороз";

        await using var xlsx = TeachersXlsxBuilder.Build(row);
        var report = await importer.ImportAsync(xlsx);

        report.Created.Should().Be(1);
        report.Errors.Should().Contain(e => e.Field == "TitleType" && e.Message.Contains("Дед мороз"));

        await using var readDb = _fx.CreateContext();
        var emp = await readDb.Employees.SingleAsync();
        emp.TitleTypeId.Should().BeNull();
    }

    /// <summary>
    /// Update path для Workload: pre-seed Class 1 з Workload (Hours5To9=10). Повторний імпорт з Hours5To9=20
    /// → блок ОНОВЛЕНО (count=1, не 2). Захист від EF-дублів через .Include() у upserter'і.
    /// </summary>
    [Fact]
    public async Task Updates_existing_workload_block_without_duplicating()
    {
        await using (var seedDb = _fx.CreateContext())
        {
            var pos = await seedDb.Positions.SingleAsync(p => p.Name == "Вчитель");
            var grade = await seedDb.TariffGrades.SingleAsync(t => t.Grade == 12);
            var emp = new Employee
            {
                TabNumber = "T001",
                FullName = "Іваненко Іван Іванович",
                TaxId = "1234567890",
                HireDate = new DateOnly(2020, 9, 1),
                Status = EmployeeStatus.Active,
            };
            emp.Positions.Add(new EmployeePosition
            {
                Position = pos,
                TariffGrade = grade,
                RateCount = 1.0m,
                HireDate = new DateOnly(2020, 9, 1),
                EffectiveFrom = new DateOnly(2020, 9, 1),
                Workload = new EmployeeWorkload { Hours5To9 = 10m },
            });
            seedDb.Employees.Add(emp);
            await seedDb.SaveChangesAsync();
        }

        await using var writeDb = _fx.CreateContext();
        var importer = BuildImporter(writeDb);

        var row = TeachersXlsxBuilder.ValidRow();
        row[TeachersColumnMap.ColHours5To9] = 20.0;

        await using var xlsx = TeachersXlsxBuilder.Build(row);
        var report = await importer.ImportAsync(xlsx);

        report.Updated.Should().Be(1);
        report.Errors.Should().BeEmpty();

        await using var readDb = _fx.CreateContext();
        var workloads = await readDb.Set<EmployeeWorkload>().ToListAsync();
        workloads.Should().HaveCount(1);
        workloads[0].Hours5To9.Should().Be(20m);
    }

    /// <summary>
    /// Маленький DRY-хелпер: збирає Importer з його залежностями на переданий DbContext.
    /// </summary>
    private static TeachersImporter BuildImporter(Infrastructure.Data.AppDbContext db) =>
        new(new TeachersParser(), new EmployeeUpserter(db), new TeachersPositionUpserter(db), db);
}
