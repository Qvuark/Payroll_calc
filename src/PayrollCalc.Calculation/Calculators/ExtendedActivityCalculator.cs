using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Позаурочна робота (ГПД/ПКР) — власний блок із 4 надбавок (відомість AE/AF + AG/AH/AI):
/// база (тариф/дільник×год) → підвищення №22 (×40%) → вислуга (×%) → престижність (×20%).
/// Вислуга й престижність рахуються від (база+№22), а не від основного окладу.
/// На відміну від решти калькуляторів повертає кілька компонентів; порожньо — блоку немає.
/// </summary>
public static class ExtendedActivityCalculator
{
    private const decimal PrestigePct = 0.20m;

    /// <param name="bonus22Rate">Ставка підвищення №22 (0.40) — той самий відсоток, що й №1749.</param>
    public static IEnumerable<CalcComponent> Calc(PositionCalcInput pos, decimal bonus22Rate, int normDays, decimal workedDays)
    {
        if (pos.ExtendedActivity is not { } a)
            yield break;
        // База: тариф/дільник×год. Дільник 1 опускаємо (ГПД = оклад×ставка), множник 1 теж.
        var baseAmount = a.Tariff / a.Divisor * a.Hours;
        var baseFormula = a.Divisor == 1 ? $"={Num(a.Tariff)}" : $"={Num(a.Tariff)}/{Num(a.Divisor)}";
        if (a.Hours != 1)
            baseFormula += $"*{Num(a.Hours)}";
        // Пропорція днями для обох видів (білдер ставить ProrateByDays=true):
        // у ГПД Hours несе кількість ставок, дні в ньому не закладені.
        if (a.ProrateByDays)
            (baseAmount, baseFormula) = Prorate(baseAmount, baseFormula, normDays, workedDays);

        var baseName = a.Kind == ExtendedActivityKind.Gpd ? "За ГПД" : "За ПКР";
        yield return new CalcComponent(baseName, baseAmount, baseFormula);

        // Підвищення №22 — від бази блоку (не від основного окладу).
        var post22 = baseAmount * bonus22Rate;
        yield return new CalcComponent("Підвищення №22", post22, $"={Num(baseAmount)}*{Num(bonus22Rate * 100)}%");

        // Вислуга й престижність — від суми (база+№22).
        var raised = baseAmount + post22;
        if (a.TenurePct != 0)
            yield return new CalcComponent("Вислуга ГПД/ПКР", raised * a.TenurePct, $"={Num(raised)}*{Num(a.TenurePct * 100)}%");
        yield return new CalcComponent("Престижність ГПД/ПКР", raised * PrestigePct, $"={Num(raised)}*{Num(PrestigePct * 100)}%");
    }
}
