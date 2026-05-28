using PayrollCalc.Documents.Export;
using PayrollCalc.Documents.Import.Staff;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// DI-реєстрації Excel import/export пайплайну.
/// Lifetime по найкоротшій залежності: stateless → Singleton, тримає AppDbContext → Scoped.
/// </summary>
public static class ImportServiceCollectionExtensions
{
    /// <summary>
    /// Реєструє парсери, генератор шаблонів та upserter'и/importer'и Staff-потоку.
    /// При додаванні Teachers / GPD пайплайнів — продовжувати цей метод (одна точка входу для bootstrap).
    /// </summary>
    public static IServiceCollection AddImportServices(this IServiceCollection services)
    {
        services.AddSingleton<StaffParser>();
        services.AddSingleton<TemplateGenerator>();
        services.AddScoped<EmployeeUpserter>();
        services.AddScoped<PositionUpserter>();
        services.AddScoped<StaffImporter>();
        return services;
    }
}
