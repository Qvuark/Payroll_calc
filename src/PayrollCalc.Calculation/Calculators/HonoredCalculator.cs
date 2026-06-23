using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Надбавка «Заслужений вчитель/працівник освіти» — фіксована сума від бухгалтера (не %).
/// Лягає в колонку звання відомості (додається до надбавки за TitleType). 0 → немає.
/// База не залежить від окладу — мама зберігає готову суму в тарифікації.
/// </summary>
public static class HonoredCalculator
{
    public static CalcComponent? Calc(PositionCalcInput pos)
    {
        if (pos.HonoredAmount == 0)
            return null;

        return new CalcComponent("За звання", pos.HonoredAmount, "=" + Num(pos.HonoredAmount));
    }
}
