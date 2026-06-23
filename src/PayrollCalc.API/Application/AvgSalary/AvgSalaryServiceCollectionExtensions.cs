namespace PayrollCalc.API.Application.AvgSalary;

/// <summary>
/// DI-реєстрація сервісу середньоденної (лікарняні / відпустки / курси).
/// </summary>
public static class AvgSalaryServiceCollectionExtensions
{
    public static IServiceCollection AddAvgSalaryServices(this IServiceCollection services)
    {
        // Зараз stateless (чисті маппери), але етап 5 додасть DbContext (авто-база) → реєструємо
        // Scoped наперед, щоб уникнути captive dependency, коли Singleton триматиме Scoped DbContext.
        services.AddScoped<AvgSalaryService>();
        services.AddScoped<AvgBaseBuilder>();
        services.AddScoped<AvgBaseAutoFiller>();
        return services;
    }
}
