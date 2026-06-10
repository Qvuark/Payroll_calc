using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Вислуга бібліотекаря — відсоток від обчисленого окладу (відомість V = J×%).
/// Відсоток за стажем, ті самі пороги що у вчителів (3/10/20 років → 10/20/30%) —
/// бере готовий TenurePct ставки. Нова людина без стажу → 0 → надбавки немає.
/// База — оклад цієї ставки (не сирий тариф). Прапорець HasLibrarianTenure; false → немає.
/// </summary>
public static class LibrarianTenureCalculator
{
    /// <param name="oklad">Сума окладу цієї ставки (результат OkladCalculator).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal oklad)
    {
        if (!pos.HasLibrarianTenure || pos.TenurePct == 0)
            return null;

        var amount = oklad * pos.TenurePct;
        return new CalcComponent("Вислуга бібліотекаря", amount, $"={Num(oklad)}*{Num(pos.TenurePct * 100)}%");
    }
}
