using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за нічні години (відомість AO, сторож) = тариф / місячна_норма × нічні_години × 40%.
/// Місячна норма годин = норма_робочих_днів × 8 (8-годинна зміна). Ставка 40% із SystemParams. 0 годин → немає.
/// </summary>
public static class NightShiftCalculator
{
    /// <summary>
    /// Тривалість робочої зміни в годинах — множник норми днів для місячної норми годин.
    /// </summary>
    private const int ShiftHours = 8;

    /// <param name="rate">Ставка нічних із SystemParams (0.40).</param>
    /// <param name="normDays">Норма робочих днів місяця — основа місячної норми годин (днів × 8).</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal rate, int normDays)
    {
        if (pos.NightHours == 0 || normDays == 0)
            return null;

        var monthlyHourNorm = normDays * ShiftHours;
        var amount = pos.Oklad / monthlyHourNorm * pos.NightHours * rate;
        var formula = $"={Num(pos.Oklad)}/{monthlyHourNorm}*{Num(pos.NightHours)}*{Num(rate * 100)}%";
        return new CalcComponent(ComponentNames.NightShift, amount, formula);
    }
}
