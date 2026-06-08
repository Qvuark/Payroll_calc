using System.Globalization;

namespace PayrollCalc.Calculation;

/// <summary>
/// Форматування чисел для Excel-формул: завжди крапка-роздільник (InvariantCulture),
/// щоб Excel не сприйняв кому за роздільник аргументів функції.
/// </summary>
internal static class CalcFormat
{
    /// <summary>
    /// Число у вигляді тексту для формули: крапка-роздільник + без хвостових нулів
    /// ("4198.5", "40", а не "4198,5" чи "40.00"). Впливає лише на текст формули, не на суми.
    /// </summary>
    public static string Num(decimal value) => value.ToString("0.############", CultureInfo.InvariantCulture);

    /// <summary>
    /// Пропорція за неповний місяць: множить суму на відпрацьовано/норму й дописує "/норма*відпрац" до формули.
    /// Повний місяць (відпрац = норма) лишає суму й формулу без змін — щоб формула була чистою.
    /// </summary>
    public static (decimal amount, string formula) Prorate(decimal amount, string formula, int normDays, decimal workedDays)
        => workedDays == normDays
            ? (amount, formula)
            : (amount * workedDays / normDays, formula + $"/{Num(normDays)}*{Num(workedDays)}");
}
