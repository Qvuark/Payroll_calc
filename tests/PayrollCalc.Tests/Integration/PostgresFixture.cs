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
    // Один NpgsqlDataSource на весь life-cycle fixture'и. Інакше CreateContext() будував би
    // новий DataSource на кожний виклик → EF реєструє новий internal IServiceProvider →
    // після 20 інстансів кидає ManyServiceProvidersCreatedWarning як помилку.
    private NpgsqlDataSource? _dataSource;

    /// <summary>
    /// Connection string на запущений контейнер. Порт рандомний — Testcontainers
    /// мапить його на вільний порт хоста, щоб паралельні тести не конфліктували.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Викликається xUnit'ом ОДИН раз перед усіма тестами класу.
    /// Стартує контейнер, будує спільний DataSource, накатує EF-міграції, сіє довідники.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _dataSource = new NpgsqlDataSourceBuilder(ConnectionString)
            .EnableDynamicJson()
            .Build();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }

    /// <summary>
    /// Викликається xUnit'ом ОДИН раз після всіх тестів класу. Зупиняє контейнер + DataSource.
    /// Disposable pattern — гарантовано викликається навіть якщо тести впали.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_dataSource is not null) await _dataSource.DisposeAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Створює свіжий AppDbContext на спільному DataSource. У тестах роби using/await using
    /// щоб з'єднання поверталися у pool. EnableDynamicJson обов'язковий для List&lt;string&gt;
    /// у jsonb (Position.ExcelAliases, TitleType.ExcelAliases) — дзеркалить Program.cs.
    /// </summary>
    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dataSource!)
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
