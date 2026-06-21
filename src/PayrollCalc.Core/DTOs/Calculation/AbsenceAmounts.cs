namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Суми подій відсутності за місяць (лікарняні/відпускні/курси), уже пораховані середньоденною.
/// Білдер дістає події з БД і складає; рушій лише додає їх компонентами. Дні відсутності тут НЕ
/// лежать — вони лишаються в табелі (worked_days), подія дає тільки гроші.
/// </summary>
public record AbsenceAmounts
{
    /// <summary>
    /// Лікарняні за рахунок роботодавця, перші 5 днів (відомість AL).
    /// </summary>
    public decimal SickEmployer { get; init; }
    /// <summary>
    /// Лікарняні за рахунок ФСС (відомість AM). Зменшує базу профспілкового внеску.
    /// </summary>
    public decimal SickFss { get; init; }
    /// <summary>
    /// Відпускні — щорічна / навчальна (відомість AZ).
    /// </summary>
    public decimal Vacation { get; init; }
    /// <summary>
    /// Компенсація за невикористану відпустку (відомість AQ).
    /// </summary>
    public decimal VacationCompensation { get; init; }
    /// <summary>
    /// Оплата за час курсів підвищення кваліфікації (відомість AT).
    /// </summary>
    public decimal Courses { get; init; }
}
