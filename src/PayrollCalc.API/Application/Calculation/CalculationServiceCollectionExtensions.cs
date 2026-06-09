using PayrollCalc.Calculation;
using PayrollCalc.Core.Interfaces;

namespace PayrollCalc.API.Application.Calculation;

/// <summary>
/// DI-реєстрації розрахункового пайплайну (рушій + білдер + оркестратор).
/// </summary>
public static class CalculationServiceCollectionExtensions
{
    public static IServiceCollection AddCalculationServices(this IServiceCollection services)
    {
        // Рушій stateless (чиста функція) → Singleton; білдер/оркестратор тримають DbContext → Scoped.
        services.AddSingleton<IPayrollCalculator, PayrollCalculator>();
        services.AddScoped<CalcInputBuilder>();
        services.AddScoped<PayrollCalculationService>();
        return services;
    }
}
