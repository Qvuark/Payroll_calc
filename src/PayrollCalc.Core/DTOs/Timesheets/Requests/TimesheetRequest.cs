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
    /// Перерахунок. Може бути відʼємним — утримання минулої переплати.
    /// </summary>
    [Range(-1000000.0, 1000000.0)] public decimal Recalculation { get; set; } = decimal.Zero;
    /// <summary>
    /// Інші ручні коригування. Може бути відʼємним.
    /// </summary>
    [Range(-1000000.0, 1000000.0)] public decimal OtherManual { get; set; } = decimal.Zero;

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
        entity.NightHours = Round(NightHours);
        entity.ReplacementHours = Round(ReplacementHours);
        entity.HolidayAmount = Round(HolidayAmount);
        entity.Advance = Round(Advance);
        entity.EnforcementOrders = Round(EnforcementOrders);
        entity.AnnualBonus = Round(AnnualBonus);
        entity.Recalculation = Round(Recalculation);
        entity.OtherManual = Round(OtherManual);
    }
    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
