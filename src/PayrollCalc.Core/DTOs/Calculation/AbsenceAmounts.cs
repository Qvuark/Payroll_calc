namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Суми подій відсутності за місяць (лікарняні/відпускні/курси), уже пораховані середньоденною,
/// плюс сумарні робочі дні цих відсутностей. Білдер дістає події з БД і складає; рушій додає суми
/// компонентами, а дні знімає з відпрацьованих (оклад падає за час відсутності).
/// </summary>
public record AbsenceAmounts
{
    /// <summary>
    /// Робочі дні всіх відсутностей місяця разом — знімаються з відпрацьованих, тож оклад і
    /// пропорційні надбавки падають за ці дні. Компенсація відпустки сюди НЕ входить: у ці дні працює.
    /// </summary>
    public int WorkingDaysAbsent { get; init; }
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
