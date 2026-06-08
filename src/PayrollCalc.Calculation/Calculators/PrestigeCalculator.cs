using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Надбавка за престижність праці — відсоток від "окладу з підвищенням" (вчитель 20%, дир-гілка 25%).
/// База — та сама, що й у вислуги: оклад + №1749 + звання (відомість R = (N+O+P)×20%).
/// Відсоток береться зі ставки (PositionCalcInput.PrestigePct); 0 → надбавки немає.
/// </summary>
public static class PrestigeCalculator
{
    /// <param name="raisedBase">Оклад з підвищенням: сума окладу, №1749 та звання цієї ставки.</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal raisedBase)
    {
        if (pos.PrestigePct == 0)
            return null;

        var amount = raisedBase * pos.PrestigePct;
        var formula = $"={Num(raisedBase)}*{Num(pos.PrestigePct * 100)}%";
        return new CalcComponent("Престижність", amount, formula);
    }
}
