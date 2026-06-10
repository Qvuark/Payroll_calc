using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Вислуга медпрацівника — відсоток від обчисленого окладу (відомість Y = J×%).
/// Відсоток за стажем, ті самі пороги що у вчителів (3/10/20 років → 10/20/30%) —
/// бере готовий TenurePct ставки. Без стажу → 0 → надбавки немає.
/// Прапорець HasMedicTenure; false → немає.
/// </summary>
public static class MedicTenureCalculator
{
    /// <param name="oklad">Сума окладу цієї ставки (результат OkladCalculator).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal oklad)
    {
        if (!pos.HasMedicTenure || pos.TenurePct == 0)
            return null;

        var amount = oklad * pos.TenurePct;
        return new CalcComponent("Вислуга медпрацівника", amount, $"={Num(oklad)}*{Num(pos.TenurePct * 100)}%");
    }
}
