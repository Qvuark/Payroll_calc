using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Базовий оклад однієї ставки. Дві гілки за класом працівника:
/// Pedagogical → погодинний N (оклад/18×години); решта класів → фіксований J (оклад×ставки[×дир%]).
/// Якщо відпрацьовано не весь місяць — сума множиться на відпрацьовано/норма.
/// </summary>
public static class OkladCalculator
{
    /// <summary>
    /// Тижнева норма педагогічних годин на одну ставку вчителя (дільник погодинного окладу).
    /// </summary>
    private const int TeacherHourNorm = 18;
    public static CalcComponent Calc(PositionCalcInput pos, int normDays, decimal workedDays)
    {
        var (amount, formula) = pos.WorkerClass == WorkerClass.Pedagogical
            ? CalcN(pos)
            : CalcJ(pos);
        // Пропорція за відпрацьовані дні — лише коли місяць неповний (інакше формула чистіша).
        if (workedDays != normDays)
        {
            amount = amount * workedDays / normDays;
            formula += $"/{Num(normDays)}*{Num(workedDays)}";
        }
        return new CalcComponent("Оклад", amount, formula);
    }
    /// <summary>
    /// N-гілка (вчитель): оклад/18 × (навантаження + надтарифні). Напр. "=8397/18*9".
    /// </summary>
    private static (decimal amount, string formula) CalcN(PositionCalcInput pos)
    {
        var hours = pos.PedHoursWeekly + pos.AdditionalHours;
        var amount = pos.Oklad / TeacherHourNorm * hours;
        var formula = $"={Num(pos.Oklad)}/{TeacherHourNorm}*{Num(hours)}";
        return (amount, formula);
    }
    /// <summary>
    /// J-гілка (адмін/спеціаліст/МОП): оклад × ставки; для залежних посад ще × % від окладу директора.
    /// Множники-одиниці опускаємо (ставка=1, нема дир%), щоб формула лишалась читабельною.
    /// </summary>
    private static (decimal amount, string formula) CalcJ(PositionCalcInput pos)
    {
        var directorPct = pos.DirectorPct ?? 1m;
        var amount = pos.Oklad * directorPct * pos.RateCount;

        var formula = $"={Num(pos.Oklad)}";
        if (pos.DirectorPct is not null)
            formula += $"*{Num(pos.DirectorPct.Value * 100)}%";
        if (pos.RateCount != 1)
            formula += $"*{Num(pos.RateCount)}";

        return (amount, formula);
    }
}
