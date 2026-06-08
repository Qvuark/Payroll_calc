using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Надбавка №1749 — 40% окладу для педагогів (постанова КМУ №1749).
/// Лише класи Pedagogical і AdminPedagogical; спеціалісти й МОП її не мають → null.
/// Рахується від уже порахованого окладу (з пропорцією за дні), тому пропорцію не дублює.
/// </summary>
public static class Bonus1749Calculator
{
    /// <param name="oklad">Сума окладу цієї ставки (результат OkladCalculator).</param>
    /// <param name="rate">Ставка надбавки з SystemParams (0.40).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal oklad, decimal rate)
    {
        if (pos.WorkerClass is not (WorkerClass.Pedagogical or WorkerClass.AdminPedagogical))
            return null;

        var amount = oklad * rate;
        var formula = $"={Num(oklad)}*{Num(rate * 100)}%";
        return new CalcComponent("Надбавка №1749", amount, formula);
    }
}
