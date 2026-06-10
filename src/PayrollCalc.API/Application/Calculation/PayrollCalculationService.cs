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
    /// Рахує всіх працівників місяця (для відомості/прогону всіх 74). «У відпустці» рахується —
    /// його відпускні лежать у табелі. Звільнений серед місяця теж у відомості свого останнього
    /// місяця (фактичні дні + компенсація відпустки — правило бухгалтера); з наступного — випадає.
    /// Порядок — український алфавіт (collation), як у табелі та еталонній відомості.
    /// </summary>
    public async Task<IReadOnlyList<CalcResult>> RunAllAsync(int year, int month, CancellationToken ct = default)
    {
        var periodStart = new DateOnly(year, month, 1);
        var ids = await db.Employees
            .Where(e => e.Status != EmployeeStatus.Dismissed
                || (e.DismissalDate != null && e.DismissalDate >= periodStart))
            .OrderBy(e => EF.Functions.Collate(e.FullName, "uk-UA-x-icu"))
            .Select(e => e.Id)
            .ToListAsync(ct);

        // Одна транзакція на весь прогін: збій на 40-му працівнику відкочує все,
        // інакше в БД лишилася б половина місяця з новими параметрами, половина зі старими.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var results = new List<CalcResult>(ids.Count);
        foreach (var id in ids)
        {
            var r = await RunAsync(id, year, month, ct);
            if (r is not null)
                results.Add(r);
        }
        await tx.CommitAsync(ct);
        return results;
    }

    /// <summary>
    /// Upsert зведення за (EmployeeId, Year, Month): gross/податки/на руки + ручні суми + знімок параметрів + час.
    /// Повний покомпонентний розклад надбавок у БД не лягає (для відомості/листів його дає рушій наживо);
    /// ручні суми зберігаємо завжди — це аудит того, що бухгалтер вписала в цей розрахунок.
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
        calc.Pdfo = Amount(r.Deductions, ComponentNames.Pdfo);
        calc.Vz = Amount(r.Deductions, ComponentNames.Vz);
        calc.UnionFee = Amount(r.Deductions, ComponentNames.UnionFee);
        calc.NetSalary = r.NetPay;

        // Ручні суми — кожна у своє поле; ManualTotal = решта ручних без власної колонки.
        calc.SickEmployer = Amount(r.Earnings, ComponentNames.SickEmployer);
        calc.SickFss = Amount(r.Earnings, ComponentNames.SickFss);
        calc.VacationAmount = Amount(r.Earnings, ComponentNames.Vacation);
        calc.ManualTotal = Amount(r.Earnings, ComponentNames.Bonus)
            + Amount(r.Earnings, ComponentNames.Recalculation)
            + Amount(r.Earnings, ComponentNames.Holiday)
            + Amount(r.Earnings, ComponentNames.AnnualBonus);

        calc.ParamsSnapshot = JsonSerializer.Serialize(r.ParamsSnapshot);
        calc.CalculatedAt = DateTime.UtcNow;
        calc.Status = CalculationStatus.Draft;

        await db.SaveChangesAsync(ct);
    }

    private static decimal Amount(IReadOnlyList<CalcComponent> components, string name)
        => components.FirstOrDefault(c => c.Name == name)?.Amount ?? 0m;
}
