using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за завідування кабінетом — відсоток від "тариф + №1749 + звання" (відомість Z).
/// Звичайний кабінет 13%, музичний/комп'ютерний 10%, майстерня 20%. null-тип → доплати немає.
/// </summary>
public static class CabinetCalculator
{
    private const decimal PctStandard = 0.13m;
    private const decimal PctMusicOrIt = 0.10m;
    private const decimal PctWorkshop = 0.20m;

    /// <param name="bonus1749Rate">Ставка №1749 (0.40) — база = тариф×(1+ставка+звання).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal bonus1749Rate)
    {
        if (pos.Cabinet is not { } cabinet)
            return null;

        var pct = cabinet switch
        {
            CabinetType.MusicOrIT => PctMusicOrIt,
            CabinetType.Workshop => PctWorkshop,
            _ => PctStandard,
        };
        // База включає звання (підтв. мамою): Ганницька (8397+15%+40%)×13%, Куріна без звання (8397+40%)×13%.
        var raisedOklad = pos.Oklad * (1 + bonus1749Rate + pos.TitlePct);
        var amount = raisedOklad * pct;
        var formula = $"={Num(raisedOklad)}*{Num(pct * 100)}%";
        return new CalcComponent("Кабінет", amount, formula);
    }
}
