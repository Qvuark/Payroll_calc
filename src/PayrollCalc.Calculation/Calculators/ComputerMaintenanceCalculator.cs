using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за обслуговування комп'ютерної техніки — 10% від "тариф + №1749" (відомість AA).
/// Прапорець зі ставки (HasComputerMaintenance); false → доплати немає.
/// </summary>
public static class ComputerMaintenanceCalculator
{
    private const decimal Pct = 0.10m;

    /// <param name="bonus1749Rate">Ставка №1749 (0.40) — база = тариф×(1+ставка).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal bonus1749Rate)
    {
        if (!pos.HasComputerMaintenance)
            return null;

        var raisedOklad = pos.Oklad * (1 + bonus1749Rate);
        var amount = raisedOklad * Pct;
        var formula = $"={Num(raisedOklad)}*{Num(Pct * 100)}%";
        return new CalcComponent("Обслуговування комп'ютерної техніки", amount, formula);
    }
}
