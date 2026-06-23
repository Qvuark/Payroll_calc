using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата 2600 за роботу в несприятливих умовах (відомість AY, педагоги). База 2600 із SystemParams.
/// Основна (штатна) ставка дістає повну 2600 навіть за неповне навантаження (&lt;18 год), а години
/// понад ставку — пропорційно: разом 2600/18 × max(18, год). Додаткова ставка/сумісництво — завжди
/// чисто пропорційно 2600/18 × год. За неповний місяць діє пропорція по днях. Без прапорця → null.
/// </summary>
public static class Unfavorable2600Calculator
{
    private const int TeacherHourNorm = 18;

    /// <param name="unfavorableBase">База 2600 із SystemParams.</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal unfavorableBase, int normDays, decimal workedDays)
    {
        if (!pos.HasUnfavorable2600)
            return null;

        // Години доплати = пед.навантаження + надтарифні (як в окладі N і в еталоні: "2600/18*(8.5+0.5)").
        var hours = pos.PedHoursWeekly + pos.AdditionalHours;
        // Норма годин ставки масштабується кількістю ставок: повна ставка 18 год, півставки 9.
        var norm = TeacherHourNorm * pos.RateCount;

        decimal amount;
        string formula;
        if (pos.IsPrimary && hours <= norm)
        {
            // Основна (штатна) ставка: повна 2600 за ставку незалежно від годин (<18 — все одно вся сума).
            // Для часток ставки сума пропорційна: півставки → 2600×0.5 = 1300.
            amount = unfavorableBase * pos.RateCount;
            formula = pos.RateCount == 1m
                ? $"={Num(unfavorableBase)}"
                : $"={Num(unfavorableBase)}*{Num(pos.RateCount)}";
        }
        else
        {
            // Години понад ставку (основна) або вся ставка сумісника — пропорційно фактичним годинам.
            if (hours == 0)
                return null;
            amount = unfavorableBase / TeacherHourNorm * hours;
            formula = $"={Num(unfavorableBase)}/{TeacherHourNorm}*{Num(hours)}";
        }

        (amount, formula) = Prorate(amount, formula, normDays, workedDays);
        return new CalcComponent(ComponentNames.Unfavorable2600, amount, formula);
    }
}
