using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Interfaces;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Calculation;

/// <summary>
/// Оркестратор розрахунку: збирає вхід (білдер) → рахує (рушій) → зберігає зведення (Calculation).
/// Повертає повний CalcResult (усі компоненти) для перевірки/diff; у БД лягає зведення + знімок параметрів.
/// </summary>
public class PayrollCalculationService(CalcInputBuilder builder, IPayrollCalculator calculator, AppDbContext db)
{
    /// <summary>
    /// Рахує одного працівника за місяць і зберігає результат. null — працівника немає.
    /// </summary>
    public async Task<CalcResult?> RunAsync(int employeeId, int year, int month, CancellationToken ct = default)
    {
        var input = await builder.BuildAsync(employeeId, year, month, ct);
        if (input is null)
            return null;

        var result = calculator.Calculate(input);
        await PersistAsync(result, ct);
        return result;
    }

    /// <summary>
    /// Рахує всіх активних працівників за місяць (для відомості/прогону всіх 74).
    /// </summary>
    public async Task<IReadOnlyList<CalcResult>> RunAllAsync(int year, int month, CancellationToken ct = default)
    {
        var ids = await db.Employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var results = new List<CalcResult>(ids.Count);
        foreach (var id in ids)
        {
            var r = await RunAsync(id, year, month, ct);
            if (r is not null)
                results.Add(r);
        }
        return results;
    }

    /// <summary>
    /// Upsert зведення за (EmployeeId, Year, Month): gross/податки/на руки + знімок параметрів + час.
    /// Повний покомпонентний розклад у БД не лягає (для відомості/листів його дає рушій наживо).
    /// </summary>
    private async Task PersistAsync(CalcResult r, CancellationToken ct)
    {
        var calc = await db.Calculations
            .FirstOrDefaultAsync(c => c.EmployeeId == r.EmployeeId && c.Year == r.Year && c.Month == r.Month, ct);
        if (calc is null)
        {
            calc = new Core.Entities.Calculation { EmployeeId = r.EmployeeId, Year = r.Year, Month = r.Month };
            db.Calculations.Add(calc);
        }

        calc.GrossSalary = r.Gross;
        calc.Pdfo = Amount(r.Deductions, "ПДФО");
        calc.Vz = Amount(r.Deductions, "Військовий збір");
        calc.UnionFee = Amount(r.Deductions, "Профспілковий внесок");
        calc.NetSalary = r.NetPay;
        calc.ParamsSnapshot = JsonSerializer.Serialize(r.ParamsSnapshot);
        calc.CalculatedAt = DateTime.UtcNow;
        calc.Status = CalculationStatus.Draft;

        await db.SaveChangesAsync(ct);
    }

    private static decimal Amount(IReadOnlyList<CalcComponent> components, string name)
        => components.FirstOrDefault(c => c.Name == name)?.Amount ?? 0m;
}
