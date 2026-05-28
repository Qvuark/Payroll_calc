using Microsoft.EntityFrameworkCore;
using Npgsql;
using PayrollCalc.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace PayrollCalc.Tests.Integration;

/// <summary>
/// xUnit-fixture: піднімає Docker-контейнер Postgres 16 для integration-тестів.
/// Один контейнер на тестовий клас (через IClassFixture). Стартує перед першим тестом,
/// мре після останнього. Між тестами таблиці чистяться через ResetEmployeeDataAsync.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("payrollcalc_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>
    /// Connection string на запущений контейнер. Порт рандомний — Testcontainers
    /// мапить його на вільний порт хоста, щоб паралельні тести не конфліктували.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Викликається xUnit'ом ОДИН раз перед усіма тестами класу.
    /// Стартує контейнер, накатує EF-міграції, сіє довідники (Positions, TariffGrades, etc).
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }

    /// <summary>
    /// Викликається xUnit'ом ОДИН раз після всіх тестів класу. Зупиняє контейнер.
    /// Disposable pattern — гарантовано викликається навіть якщо тести впали.
    /// </summary>
    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>
    /// Створює свіжий AppDbContext на цей контейнер. У тестах роби using/await using
    /// щоб з'єднання закривались — Postgres має ліміт connection pool.
    /// EnableDynamicJson — обов'язково для List&lt;string&gt; у jsonb (Position.ExcelAliases, TitleType.ExcelAliases),
    /// дзеркалить конфіг Program.cs.
    /// </summary>
    public AppDbContext CreateContext()
    {
        var dataSource = new NpgsqlDataSourceBuilder(ConnectionString)
            .EnableDynamicJson()
            .Build();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(dataSource)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Видаляє тестові дані між запусками: Employees + EmployeePositions (cascade чистить блоки).
    /// Довідники (Positions, TariffGrades) лишаються — їх сіяли один раз у InitializeAsync.
    /// </summary>
    public async Task ResetEmployeeDataAsync()
    {
        await using var db = CreateContext();
        // TRUNCATE ... CASCADE — швидше за DELETE + автоматично чистить залежні рядки EmployeePositions.
        // RESTART IDENTITY — скидає auto-increment, щоб Id у тестах був передбачуваним (1, 2, 3...).
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE \"Employees\" RESTART IDENTITY CASCADE;");
    }
}
