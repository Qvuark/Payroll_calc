using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.AvgSalary;

/// <summary>
/// Збирає базу середньоденної з підписаної історії: сумує покомпонентні нарахування за вікно
/// (12 міс для лікарняних/відпускних/компенсації, 2 міс для курсів), беручи лише компоненти,
/// чия галочка включення в довіднику ввімкнена для цього виду події. Знаменник (дні) — окремо.
/// </summary>
public class AvgBaseBuilder(AppDbContext db)
{
    /// <summary>
    /// Числівник середньоденної: Σ підписаних нарахувань за вікно, де галочка виду = true.
    /// Повертає ще й кількість підписаних місяців у вікні — щоб авто-режим вирішив, чи достатньо
    /// історії (мало → fallback на ручну базу). Подія за (year, month); вікно — місяці перед нею.
    /// </summary>
    public async Task<AvgBaseAmount> BuildAmountAsync(int employeeId, int year, int month, AvgBaseKind kind, CancellationToken ct = default)
    {
        // Вікно закінчується місяцем ПЕРЕД подією (середньоденна — за попередні повні місяці).
        var count = kind == AvgBaseKind.Training ? 2 : 12;
        var lastOrd = year * 12 + month - 1;
        var firstOrd = lastOrd - (count - 1);
        // Довідник включення фільтруємо по колонці виду — лишаються FieldKey, що входять у базу.
        IQueryable<AvgSalaryInclusionRule> includedRules;
        switch (kind)
        {
            case AvgBaseKind.Sick:
                includedRules = db.AvgSalaryInclusionRules.Where(r => r.IncludeSick);
                break;
            case AvgBaseKind.Vacation:
                includedRules = db.AvgSalaryInclusionRules.Where(r => r.IncludeVacation);
                break;
            case AvgBaseKind.Training:
                includedRules = db.AvgSalaryInclusionRules.Where(r => r.IncludeTraining);
                break;
            case AvgBaseKind.Compensation:
                includedRules = db.AvgSalaryInclusionRules.Where(r => r.IncludeCompensation);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
        // Нарахування підписаних місяців вікна, чий FieldKey дозволений довідником.
        var amount = await (
            from cc in db.CalculationComponents
            join c in db.Calculations on cc.CalculationId equals c.Id
            join r in includedRules on cc.FieldKey equals r.FieldKey
            where c.EmployeeId == employeeId
               && c.Status == CalculationStatus.Signed
               && cc.Kind == ComponentKind.Earning
               && c.Year * 12 + c.Month >= firstOrd
               && c.Year * 12 + c.Month <= lastOrd
            select (decimal?)cc.Amount).SumAsync(ct) ?? 0m;
        // Скільки підписаних місяців реально у вікні (унікальний індекс → 1 рядок на місяць).
        var monthsFound = await db.Calculations
            .Where(c => c.EmployeeId == employeeId
                && c.Status == CalculationStatus.Signed
                && c.Year * 12 + c.Month >= firstOrd
                && c.Year * 12 + c.Month <= lastOrd)
            .CountAsync(ct);
        return new AvgBaseAmount(amount, monthsFound);
    }

    /// <summary>
    /// Виключені дні знаменника: дні у вікні, коли працівник не заробляв, що зменшують 365.
    /// Лікарняний — минулі лікарняні + відпустки за свій рахунок; відпускні/компенсація — лише
    /// відпустки за свій рахунок. Держсвята під час воєнного стану не віднімаються (мама 2026-06-22),
    /// повернути їх віднімання для відпускних після скасування воєнного стану. Вікно — 12 міс перед подією.
    /// </summary>
    public async Task<int> BuildExcludedDaysAsync(int employeeId, int year, int month, AvgBaseKind kind, CancellationToken ct = default)
    {
        var eventFirstDay = new DateOnly(year, month, 1);
        var windowStart = eventFirstDay.AddMonths(-12);
        // Відпустка без збереження зп — виключається в обох випадках (лікарняний і відпускні).
        var unpaidDays = await db.Vacations
            .Where(v => v.EmployeeId == employeeId
                && v.VacationType == VacationType.Unpaid
                && v.StartDate >= windowStart
                && v.StartDate < eventFirstDay)
            .SumAsync(v => (int?)v.CalendarDays, ct) ?? 0;
        if (kind != AvgBaseKind.Sick)
            return unpaidDays;
        // Лікарняний додатково виключає дні минулих лікарняних у вікні.
        var sickDays = await db.SickLeaves
            .Where(s => s.EmployeeId == employeeId
                && s.StartDate >= windowStart
                && s.StartDate < eventFirstDay)
            .SumAsync(s => (int?)s.DaysTotal, ct) ?? 0;
        return unpaidDays + sickDays;
    }

    /// <summary>
    /// Знаменник курсів: сума робочих днів 2 місяців перед подією з довідника work_calendar.
    /// На курсах працівник був би на роботі лише в робочі дні, тож ділимо на них, не на календарні.
    /// </summary>
    public async Task<int> BuildTrainingWorkingDaysAsync(int year, int month, CancellationToken ct = default)
    {
        var lastOrd = year * 12 + month - 1;
        var firstOrd = lastOrd - 1;
        return await db.WorkCalendars
            .Where(w => w.Year * 12 + w.Month >= firstOrd && w.Year * 12 + w.Month <= lastOrd)
            .SumAsync(w => (int?)w.WorkDays, ct) ?? 0;
    }
}
/// <summary>
/// База середньоденної: підсумована сума нарахувань і скільки підписаних місяців її дали.
/// </summary>
public record AvgBaseAmount(decimal Amount, int SignedMonths);
