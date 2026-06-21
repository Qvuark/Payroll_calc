namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Результат розрахунку відпускних / компенсації за невикористану відпустку (КМУ №100).
/// Простіший за лікарняний — без поділу школа/ФСС і без відсотка стажу: відпускні
/// платить роботодавець, завжди 100%.
/// </summary>
/// <param name="AverageDaily">Середньоденна = base / сума календарних днів за 12 міс.</param>
/// <param name="Total">Сума = середньоденна × календарні дні відпустки.</param>
public record VacationResult(decimal AverageDaily, decimal Total);
