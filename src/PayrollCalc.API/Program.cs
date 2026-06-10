using Microsoft.EntityFrameworkCore;
using Npgsql;
using PayrollCalc.API.Application.Calculation;
using PayrollCalc.API.Application.Import;
using PayrollCalc.API.Middleware;
using PayrollCalc.Infrastructure.Data;
using Serilog;
using System.Text;
// Bootstrap logger — пише в консоль до того як HostBuilder побудує DI.
// Потрібен щоб не втратити помилки startup (битий appsettings, тощо).
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
try
{
    Log.Information("Starting PayrollCalc.API");
    // Регіструємо Windows codepages (cp1251 тощо) для legacy .xls/.xlsx файлів.
    // .NET Core знає лише UTF / ASCII / ISO-8859-1 з коробки.
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    var builder = WebApplication.CreateBuilder(args);
    // Підключаємо Serilog до Host. ReadFrom.Configuration — читає секцію
    // "Serilog" з appsettings.json (sinks, levels, enrichers).
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddControllers();

    // EnableDynamicJson — обов'язково для List<string> у jsonb колонці (Position.ExcelAliases).
    // Npgsql 8+ вимагає явний opt-in для dynamic JSON сериалізації.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var dataSource = new NpgsqlDataSourceBuilder(connectionString)
        .EnableDynamicJson()
        .Build();
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dataSource));
    builder.Services.AddImportServices();
    builder.Services.AddCalculationServices();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Логує кожен HTTP request одним рядком (method, path, status, elapsed).
    // Замінює дефолтне ASP.NET request-logging, яке зашумлює лог.
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // SPA з wwwroot: UseDefaultFiles підставляє index.html на "/", далі статика.
    // Фронт на HashRouter — серверний fallback для маршрутів не потрібен.
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PayrollCalc.API terminated unexpectedly");
}
finally
{
    // Flush буферів Serilog перед виходом — щоб останні логи дійшли до файлу.
    Log.CloseAndFlush();
}
