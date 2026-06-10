namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Ручні разові суми за місяць — мама вписує, рушій їх не рахує (нема формули або зовнішнє джерело).
/// За замовчуванням 0 (більшість місяців порожні). Поля ростуть у міру потреби.
/// </summary>
public record ManualAdjustments
{
    /// <summary>
    /// Премія (відомість BB).
    /// </summary>
    public decimal Bonus { get; init; }
    /// <summary>
    /// Аванс — виплачено наперед, потім утримується (відомість BI).
    /// </summary>
    public decimal Advance { get; init; }
    /// <summary>
    /// Лікарняні за рахунок роботодавця, перші дні (відомість AL).
    /// </summary>
    public decimal SickEmployer { get; init; }
    /// <summary>
    /// Лікарняні за рахунок ФСС (відомість AM). Зменшує базу профспілкового внеску.
    /// </summary>
    public decimal SickFss { get; init; }
    /// <summary>
    /// Перерахунок за минулі періоди (відомість AS).
    /// </summary>
    public decimal Recalculation { get; init; }
    /// <summary>
    /// Відпускні (відомість AZ).
    /// </summary>
    public decimal Vacation { get; init; }
    /// <summary>
    /// Святкові (відомість AP). Ручна сума; авто-розрахунок від святкових годин — пізніше.
    /// </summary>
    public decimal Holiday { get; init; }
    /// <summary>
    /// Щорічна винагорода вчителям, ст.57 ЗУ "Про освіту" (відомість AX).
    /// </summary>
    public decimal AnnualBonus { get; init; }
    /// <summary>
    /// Утримання за виконавчими листами (відомість BG).
    /// </summary>
    public decimal EnforcementOrders { get; init; }
    /// <summary>
    /// Позакласна робота з фізвиховання (відомість AC). Ручна сума — формули в еталоні немає.
    /// </summary>
    public decimal PhysEducation { get; init; }
    /// <summary>
    /// Компенсація за невикористану відпустку (відомість AQ).
    /// </summary>
    public decimal VacationCompensation { get; init; }
    /// <summary>
    /// Оплата простою (відомість AR).
    /// </summary>
    public decimal Downtime { get; init; }
    /// <summary>
    /// Оплата за час курсів підвищення кваліфікації (відомість AT).
    /// </summary>
    public decimal Courses { get; init; }
    /// <summary>
    /// Індексація зарплати (відомість AV). Зараховується в базу доплати до МЗП.
    /// </summary>
    public decimal Indexation { get; init; }
}
