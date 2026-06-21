namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Результат розрахунку курсів підвищення кваліфікації (КМУ №100). Як відпускні —
/// без поділу школа/ФСС і без відсотка стажу, але знаменник у РОБОЧИХ днях (не календарних).
/// </summary>
/// <param name="AverageDaily">Середньоденна = base / робочі дні 2 місяців.</param>
/// <param name="Total">Сума = середньоденна × робочі дні відсутності на курсах.</param>
public record TrainingResult(decimal AverageDaily, decimal Total);
