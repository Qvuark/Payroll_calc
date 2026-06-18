using PayrollCalc.Documents.Export;
using PayrollCalc.Documents.Import.Staff;
using PayrollCalc.Documents.Import.Teachers;
using PayrollCalc.Documents.Import.Timesheet;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// DI-реєстрації Excel import/export пайплайну.
/// Lifetime по найкоротшій залежності: stateless → Singleton, тримає AppDbContext → Scoped.
/// </summary>
public static class ImportServiceCollectionExtensions
{
    /// <summary>
    /// Реєструє парсери, генератор шаблонів та upserter'и/importer'и Staff, Teachers і Timesheet потоків.
    /// При додаванні нового Excel-потоку — продовжувати цей метод (одна точка входу для bootstrap).
    /// </summary>
    public static IServiceCollection AddImportServices(this IServiceCollection services)
    {
        services.AddSingleton<StaffParser>();
        services.AddSingleton<TeachersParser>();
        services.AddSingleton<TimesheetParser>();
        services.AddSingleton<TemplateGenerator>();
        services.AddScoped<EmployeeUpserter>();
        services.AddScoped<PositionUpserter>();
        services.AddScoped<TeachersPositionUpserter>();
        services.AddScoped<StaffImporter>();
        services.AddScoped<TeachersImporter>();
        services.AddScoped<TimesheetTemplateService>();
        services.AddScoped<TimesheetUpserter>();
        services.AddScoped<TimesheetImporter>();
        return services;
    }
}
