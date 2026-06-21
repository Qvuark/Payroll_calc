namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Результат розрахунку лікарняного: середньоденна + дні + суми. Лікарняний роздвоюється
/// на частину роботодавця (перші 5 днів) і ФСС (решта), тому суми й дні тримаємо окремо —
/// ФСС-частина виключається з бази профспілки, роботодавцеву проводимо в gross школи.
/// </summary>
/// <param name="AverageDaily">Середньоденна = base / (365 − дні відсутності).</param>
/// <param name="DaysEmployer">Дні за рахунок роботодавця (перші 5).</param>
/// <param name="DaysFss">Дні за рахунок ФСС (понад 5).</param>
/// <param name="AmountEmployer">Сума роботодавця = середньоденна × дні_роб × %.</param>
/// <param name="AmountFss">Сума ФСС = середньоденна × дні_фсс × %.</param>
/// <param name="Total">Разом = роботодавець + ФСС.</param>
public record SickLeaveResult(
    decimal AverageDaily,
    int DaysEmployer,
    int DaysFss,
    decimal AmountEmployer,
    decimal AmountFss,
    decimal Total);
