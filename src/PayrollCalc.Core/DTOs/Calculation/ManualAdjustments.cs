namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Ручні разові суми за місяць — бухгалтер вписує, рушій їх не рахує (нема формули або зовнішнє джерело).
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
    /// Перерахунок за минулі періоди (відомість AS).
    /// </summary>
    public decimal Recalculation { get; init; }
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
    /// Оплата простою (відомість AR).
    /// </summary>
    public decimal Downtime { get; init; }
    /// <summary>
    /// Індексація зарплати (відомість AV). Зараховується в базу доплати до МЗП.
    /// </summary>
    public decimal Indexation { get; init; }
    /// <summary>
    /// Доплата за несприятливі умови праці — ручна надбавка понад почасову (відомість AY).
    /// Для індивідуальних рішень бухгалтера: плоска 2600, її частина (2600/2, 2600/4) тощо.
    /// Сумується з почасовою доплатою у тій самій колонці AY.
    /// </summary>
    public decimal UnfavorableManual { get; init; }
}
