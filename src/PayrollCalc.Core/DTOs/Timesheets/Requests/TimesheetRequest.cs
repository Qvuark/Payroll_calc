using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Timesheets.Requests;

/// <summary>
/// Запит на запис табеля — місячний змінний шар одного працівника.
/// Натуральний ключ (EmployeeId, Year, Month) використовується для upsert.
/// Разові one-off виплати теж тут, але у шаблон імпорту не йдуть — лише через CRUD.
/// </summary>
public class TimesheetRequest
{
    [Range(1, int.MaxValue)] public int EmployeeId { get; set; }
    [Range(2020, 2100)] public int Year { get; set; }
    [Range(1, 12)] public int Month { get; set; }
    /// <summary>
    /// Відпрацьовано днів за місяць — головний вхід окладного розрахунку.
    /// </summary>
    [Range(0.0, 31.0)] public decimal WorkedDays { get; set; } = decimal.Zero;
    /// <summary>
    /// Відпрацьовані години за місяць — лише для погодинних посад (сторож).
    /// </summary>
    [Range(0.0, 744.0)] public decimal WorkedHours { get; set; } = decimal.Zero;
    [Range(0.0, 744.0)] public decimal NightHours { get; set; } = decimal.Zero;
    [Range(0.0, 744.0)] public decimal ReplacementHours { get; set; } = decimal.Zero;
    [Range(0.0, 1000000.0)] public decimal HolidayAmount { get; set; } = decimal.Zero;
    [Range(0.0, 1000000.0)] public decimal Advance { get; set; } = decimal.Zero;
    /// <summary>
    /// Утримання за виконавчими листами.
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal EnforcementOrders { get; set; } = decimal.Zero;
    [Range(0.0, 1000000.0)] public decimal AnnualBonus { get; set; } = decimal.Zero;
    /// <summary>
    /// Премія за місяць (відомість BB).
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal Bonus { get; set; } = decimal.Zero;
    /// <summary>
    /// Лікарняні за рахунок роботодавця, перші 5 днів (відомість AL).
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal SickEmployer { get; set; } = decimal.Zero;
    /// <summary>
    /// Лікарняні за рахунок ФСС (відомість AM). Зменшує базу профспілкового внеску.
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal SickFss { get; set; } = decimal.Zero;
    /// <summary>
    /// Відпускні (відомість AZ).
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal Vacation { get; set; } = decimal.Zero;
    /// <summary>
    /// Перерахунок. Може бути відʼємним — утримання минулої переплати.
    /// </summary>
    [Range(-1000000.0, 1000000.0)] public decimal Recalculation { get; set; } = decimal.Zero;
    /// <summary>
    /// Позакласна робота з фізвиховання (відомість AC).
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal PhysEducation { get; set; } = decimal.Zero;
    /// <summary>
    /// Компенсація за невикористану відпустку (відомість AQ).
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal VacationCompensation { get; set; } = decimal.Zero;
    /// <summary>
    /// Оплата простою (відомість AR).
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal Downtime { get; set; } = decimal.Zero;
    /// <summary>
    /// Оплата за час курсів підвищення кваліфікації (відомість AT).
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal Courses { get; set; } = decimal.Zero;
    /// <summary>
    /// Індексація зарплати (відомість AV).
    /// </summary>
    [Range(0.0, 1000000.0)] public decimal Indexation { get; set; } = decimal.Zero;

    /// <summary>
    /// Маппінг Request → нова entity (insert-шлях upsert-у). Ставить ключ і делегує значення в ApplyTo.
    /// </summary>
    /// <param name="request">Дані запиту.</param>
    /// <returns>Новий Timesheet, готовий до Add у DbContext.</returns>
    public static Timesheet FromRequest(TimesheetRequest request)
    {
        var timesheet = new Timesheet
        {
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            Month = request.Month
        };
        request.ApplyTo(timesheet);
        return timesheet;
    }
    /// <summary>
    /// Копіює змінні поля на існуючу entity (update-шлях upsert-у). Ключ не чіпає.
    /// Кожне значення округлюється до 2 знаків (доменне правило).
    /// </summary>
    /// <param name="entity">Існуючий Timesheet для оновлення.</param>
    public void ApplyTo(Timesheet entity)
    {
        entity.WorkedDays = Round(WorkedDays);
        entity.WorkedHours = Round(WorkedHours);
        entity.NightHours = Round(NightHours);
        entity.ReplacementHours = Round(ReplacementHours);
        entity.HolidayAmount = Round(HolidayAmount);
        entity.Advance = Round(Advance);
        entity.EnforcementOrders = Round(EnforcementOrders);
        entity.AnnualBonus = Round(AnnualBonus);
        entity.Bonus = Round(Bonus);
        entity.SickEmployer = Round(SickEmployer);
        entity.SickFss = Round(SickFss);
        entity.Vacation = Round(Vacation);
        entity.Recalculation = Round(Recalculation);
        entity.PhysEducation = Round(PhysEducation);
        entity.VacationCompensation = Round(VacationCompensation);
        entity.Downtime = Round(Downtime);
        entity.Courses = Round(Courses);
        entity.Indexation = Round(Indexation);
    }
    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
