using PayrollCalc.Calculation.AvgSalary;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.API.Application.AvgSalary;

/// <summary>
/// Проводка подій середньоденної: бере подію (лікарняний / відпустка / курси),
/// запускає відповідний калькулятор і розкладає результат назад у поля події.
/// Manual-режим: база береться з самої події (бухгалтер вписала). Авто-збір бази
/// з історії за 12/2 міс — етап 5, тоді сюди прийде DbContext. SaveChanges робить контролер.
/// </summary>
public class AvgSalaryService
{
    /// <summary>
    /// Лікарняний за КМУ №1266. Відсоток виплати: ручний override, якщо заданий,
    /// інакше виводиться зі страхового стажу. base_days = 365 − виключені дні рахуємо тут
    /// (калькулятор його не повертає). Якщо суму роботодавця/ФСС вписали руками
    /// (факт розійшовся з формулою) — беремо ручну; середньоденна лишається довідковою.
    /// </summary>
    public void ApplySick(SickLeave e)
    {
        var pct = e.PaymentPct > 0 ? e.PaymentPct : SickLeaveCalculator.PaymentPct(e.InsuranceSeniorityYrs);
        var r = SickLeaveCalculator.Calc(e.BaseAmount, e.BaseExcludedDays, e.DaysTotal, pct);
        e.PaymentPct = pct;
        e.BaseDays = 365 - e.BaseExcludedDays;
        e.AverageDaily = r.AverageDaily;
        e.DaysEmployer = r.DaysEmployer;
        e.DaysFss = r.DaysFss;
        e.AmountEmployer = e.OverrideAmountEmployer ?? r.AmountEmployer;
        e.AmountFss = e.OverrideAmountFss ?? r.AmountFss;
        e.TotalAmount = e.AmountEmployer + e.AmountFss;
    }
    /// <summary>
    /// Відпустка / компенсація за КМУ №100. Неоплачувані типи (без збереження зарплати,
    /// по догляду за дитиною) виплати не дають — виходи лишаються null. Решта (щорічна,
    /// навчальна, компенсація) рахуються однією формулою.
    /// </summary>
    public void ApplyVacation(Vacation e)
    {
        switch (e.VacationType)
        {
            case VacationType.Unpaid:
            case VacationType.ChildCare:
                e.AverageDaily = null;
                e.TotalAmount = null;
                return;
            default:
                break;
        }
        var r = VacationCalculator.Calc(e.BaseAmount ?? 0m, e.BaseDays ?? 0, e.CalendarDays);
        e.AverageDaily = r.AverageDaily;
        e.TotalAmount = e.OverrideTotalAmount ?? r.Total;
    }
    /// <summary>
    /// Курси підвищення кваліфікації за КМУ №100: період 2 місяці, знаменник у робочих днях.
    /// </summary>
    public void ApplyTraining(TrainingLeave e)
    {
        var r = TrainingCalculator.Calc(e.BaseAmount, e.BaseWorkingDays, e.WorkingDaysAbsent);
        e.AverageDaily = r.AverageDaily;
        e.TotalAmount = e.OverrideTotalAmount ?? r.Total;
    }
}
