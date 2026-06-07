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
    /// Утримання за виконавчими листами (відомість BG).
    /// </summary>
    public decimal EnforcementOrders { get; init; }
}
