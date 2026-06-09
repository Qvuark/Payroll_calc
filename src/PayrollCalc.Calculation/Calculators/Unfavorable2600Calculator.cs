using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за роботу в несприятливих умовах (відомість AY, педагоги) = 2600 + 2600/18 × пед.години.
/// База 2600 із SystemParams; за неповний місяць діє пропорція. false → доплати немає.
/// ⚠️ В еталоні є й інші варіанти (2600+2600/2, 2600/2+...) — поки головна формула, звірити на даних.
/// </summary>
public static class Unfavorable2600Calculator
{
    private const int TeacherHourNorm = 18;

    /// <param name="unfavorableBase">База 2600 із SystemParams.</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal unfavorableBase, int normDays, decimal workedDays)
    {
        if (!pos.HasUnfavorable2600)
            return null;

        var amount = unfavorableBase + unfavorableBase / TeacherHourNorm * pos.PedHoursWeekly;
        var formula = $"={Num(unfavorableBase)}+{Num(unfavorableBase)}/{TeacherHourNorm}*{Num(pos.PedHoursWeekly)}";

        (amount, formula) = Prorate(amount, formula, normDays, workedDays);
        return new CalcComponent("Несприятливі умови (2600)", amount, formula);
    }
}
