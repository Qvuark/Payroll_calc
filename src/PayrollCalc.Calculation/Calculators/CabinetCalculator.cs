using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за завідування кабінетом — відсоток від "тариф + №1749" (відомість Z).
/// Звичайний кабінет 13%, музичний/комп'ютерний 10%. null-тип → доплати немає.
/// </summary>
public static class CabinetCalculator
{
    private const decimal PctStandard = 0.13m;
    private const decimal PctMusicOrIt = 0.10m;

    /// <param name="bonus1749Rate">Ставка №1749 (0.40) — база = тариф×(1+ставка).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal bonus1749Rate)
    {
        if (pos.Cabinet is not { } cabinet)
            return null;

        // Майстерня (Workshop): % не підтверджений — поки рахуємо як звичайний кабінет (техдолг, звірити з мамою).
        var pct = cabinet == CabinetType.MusicOrIT ? PctMusicOrIt : PctStandard;
        var raisedOklad = pos.Oklad * (1 + bonus1749Rate);
        var amount = raisedOklad * pct;
        var formula = $"={Num(raisedOklad)}*{Num(pct * 100)}%";
        return new CalcComponent("Кабінет", amount, formula);
    }
}
