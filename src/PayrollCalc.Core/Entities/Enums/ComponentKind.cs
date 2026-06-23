namespace PayrollCalc.Core.Entities.Enums;
/// <summary>
/// Бік компонента розрахунку: нарахування (додає до gross) чи утримання (віднімає).
/// Дозволяє пересобрати нарахування й утримання окремо з покомпонентного збереження.
/// </summary>
public enum ComponentKind
{
    Earning,
    Deduction
}
