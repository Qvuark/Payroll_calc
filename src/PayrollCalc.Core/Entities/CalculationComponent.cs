using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;
/// <summary>
/// Один рядок розкладу розрахунку — окреме нарахування чи утримання за місяць.
/// Зберігаємо покомпонентно (не агрегатом), щоб авто-база середньоденної могла
/// взяти потрібні компоненти за правилами включення (FieldKey ↔ inclusion_rules).
/// </summary>
public class CalculationComponent
{
    public int Id { get; set; }
    public int CalculationId { get; set; }
    public Calculation? Calculation { get; set; }
    public ComponentKind Kind { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public decimal Amount { get; set; } = decimal.Zero;
}
