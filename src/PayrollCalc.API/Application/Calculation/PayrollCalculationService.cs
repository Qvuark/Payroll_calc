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
        // Гуртова збірка входів (кілька запитів на весь місяць) замість запиту-на-людину — рушій
        // далі чистий CPU, тож вузьке місце прогону зникає. Порядок — український алфавіт (з білдера).
        var inputs = await builder.BuildAllAsync(year, month, ct);

        // Одна транзакція на весь прогін: збій на 40-му працівнику відкочує все,
        // інакше в БД лишилася б половина місяця з новими параметрами, половина зі старими.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var results = new List<CalcResult>(inputs.Count);
        foreach (var input in inputs)
        {
            var result = calculator.Calculate(input);
            await PersistAsync(result, ct);
            results.Add(result);
        }
        await tx.CommitAsync(ct);
        return results;
    }

    /// <summary>
    /// Підписує розрахунок: фіксує місяць як факт — його суми йдуть в авто-базу середньоденної,
    /// і перепрогон більше його не перетирає (замок у PersistAsync). false — розрахунку з таким id немає.
    /// </summary>
    public async Task<bool> SignAsync(int calculationId, CancellationToken ct = default)
    {
        var calc = await db.Calculations.FirstOrDefaultAsync(c => c.Id == calculationId, ct);
        if (calc is null)
            return false;
        calc.Status = CalculationStatus.Signed;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Знімає підпис (аварійний клапан): повертає місяць у чернетку, дозволяючи перепрогон/правку.
    /// На совісті бухгалтера — для зданого державі періоду правильний шлях перерахунок. false — немає такого id.
    /// </summary>
    public async Task<bool> UnsignAsync(int calculationId, CancellationToken ct = default)
    {
        var calc = await db.Calculations.FirstOrDefaultAsync(c => c.Id == calculationId, ct);
        if (calc is null)
            return false;
        calc.Status = CalculationStatus.Draft;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Підписує весь місяць: усі чернетки за (Year, Month) → Signed. Підпис = «місяць перевірено»;
    /// з цього моменту суми йдуть в авто-базу середньоденної. Повертає, скільки розрахунків підписано.
    /// </summary>
    public async Task<int> SignMonthAsync(int year, int month, CancellationToken ct = default)
    {
        return await db.Calculations
            .Where(c => c.Year == year && c.Month == month && c.Status == CalculationStatus.Draft)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CalculationStatus.Signed), ct);
    }

    /// <summary>
    /// Знімає підпис з усього місяця (аварійний клапан): усі Signed за (Year, Month) → Draft,
    /// дозволяючи перепрогон/правку. Повертає, скільки розрахунків розблоковано.
    /// </summary>
    public async Task<int> UnsignMonthAsync(int year, int month, CancellationToken ct = default)
    {
        return await db.Calculations
            .Where(c => c.Year == year && c.Month == month && c.Status == CalculationStatus.Signed)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, CalculationStatus.Draft), ct);
    }

    /// <summary>
    /// Стан підпису місяця: скільки всього розрахунків збережено й скільки з них підписано.
    /// UI показує «підписано N/усього» і яку кнопку давати (підписати чи зняти).
    /// </summary>
    public async Task<MonthSignStatus> GetMonthStatusAsync(int year, int month, CancellationToken ct = default)
    {
        var total = await db.Calculations.CountAsync(c => c.Year == year && c.Month == month, ct);
        var signed = await db.Calculations.CountAsync(c => c.Year == year && c.Month == month && c.Status == CalculationStatus.Signed, ct);
        return new MonthSignStatus(total, signed);
    }

    /// <summary>
    /// Upsert зведення за (EmployeeId, Year, Month): gross/податки/на руки + ручні суми + знімок параметрів + час,
    /// плюс повний покомпонентний розклад рядками (звідси авто-база середньоденної бере суми минулих місяців).
    /// Підписаний місяць — заморожений факт: перепрогон його не чіпає (помилку правлять перерахунком далі).
    /// </summary>
    private async Task PersistAsync(CalcResult r, CancellationToken ct)
    {
        var calc = await db.Calculations
            .Include(c => c.Components)
            .FirstOrDefaultAsync(c => c.EmployeeId == r.EmployeeId && c.Year == r.Year && c.Month == r.Month, ct);

        // Замок: підписаний місяць заморожений — не перетираємо. Без нього перепрогон відомості
        // затер би факт, з якого рахується середньоденна.
        if (calc is { Status: CalculationStatus.Signed })
            return;

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

        // Покомпонентний розклад: знести старі рядки й записати поточні (перепрогон чернетки
        // дає чистий розклад без задвоєння). Агрегати вище лишаємо — їх читає експорт відомості.
        db.CalculationComponents.RemoveRange(calc.Components);
        calc.Components.Clear();
        foreach (var c in r.Earnings)
            calc.Components.Add(new Core.Entities.CalculationComponent { Kind = ComponentKind.Earning, FieldKey = c.Name, Amount = c.Amount });
        foreach (var c in r.Deductions)
            calc.Components.Add(new Core.Entities.CalculationComponent { Kind = ComponentKind.Deduction, FieldKey = c.Name, Amount = c.Amount });

        calc.ParamsSnapshot = JsonSerializer.Serialize(r.ParamsSnapshot);
        calc.CalculatedAt = DateTime.UtcNow;
        calc.Status = CalculationStatus.Draft;

        await db.SaveChangesAsync(ct);
    }

    private static decimal Amount(IReadOnlyList<CalcComponent> components, string name)
        => components.FirstOrDefault(c => c.Name == name)?.Amount ?? 0m;
}

/// <summary>
/// Стан підпису місяця для UI: усього збережених розрахунків і скільки підписано.
/// </summary>
public record MonthSignStatus(int Total, int Signed);
