using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Доплата за роботу в інклюзивних класах (відомість U) — 20%, але по-різному за класом:
/// учитель — погодинно (тариф+1749+звання)×20%/18×години; адмін (директор/завуч) — ціла
/// (адмін.оклад+1749)×20% без поділу на години (підтв. мамою). Пропорція за неповний місяць.
/// 0 інклюзивних годин → доплати немає (для адміна години = прапорець участі).
/// </summary>
public static class InclusiveCalculator
{
    private const int TeacherHourNorm = 18;
    private const decimal Pct = 0.20m;

    /// <param name="okladAmount">Обчислений оклад ставки (для адмінів — з урахуванням дир.%).</param>
    /// <param name="bonus1749Rate">Ставка №1749 (0.40).</param>
    public static CalcComponent? Calc(
        PositionCalcInput pos, decimal okladAmount, decimal bonus1749Rate, int normDays, decimal workedDays)
    {
        if (pos.InclusiveHours == 0)
            return null;

        var (amount, formula) = pos.WorkerClass == WorkerClass.AdminPedagogical
            ? CalcAdmin(okladAmount, bonus1749Rate)
            : CalcTeacher(pos, bonus1749Rate);

        (amount, formula) = Prorate(amount, formula, normDays, workedDays);
        return new CalcComponent("Інклюзивні класи", amount, formula);
    }

    /// <summary>
    /// Адмін: 20% від (оклад+1749) цілком, без годин. Напр. Скирда (10410+40%)×20% = 2914.8.
    /// </summary>
    private static (decimal, string) CalcAdmin(decimal okladAmount, decimal bonus1749Rate)
    {
        var raised = okladAmount * (1 + bonus1749Rate);
        return (raised * Pct, $"={Num(raised)}*{Num(Pct * 100)}%");
    }

    /// <summary>
    /// Учитель: (тариф+1749+звання)×20% / 18 × інклюзивні години.
    /// </summary>
    private static (decimal, string) CalcTeacher(PositionCalcInput pos, decimal bonus1749Rate)
    {
        var raised = pos.Oklad * (1 + bonus1749Rate + pos.TitlePct);
        var amount = raised * Pct / TeacherHourNorm * pos.InclusiveHours;
        return (amount, $"={Num(raised)}*{Num(Pct * 100)}%/{TeacherHourNorm}*{Num(pos.InclusiveHours)}");
    }
}
