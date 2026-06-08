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
}
