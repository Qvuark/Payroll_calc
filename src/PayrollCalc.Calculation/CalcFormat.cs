using System.Globalization;

namespace PayrollCalc.Calculation;

/// <summary>
/// Форматування чисел для Excel-формул: завжди крапка-роздільник (InvariantCulture),
/// щоб Excel не сприйняв кому за роздільник аргументів функції.
/// </summary>
internal static class CalcFormat
{
    /// <summary>
    /// Число у вигляді тексту для формули: "4198.5", а не локальне "4198,5".
    /// </summary>
    public static string Num(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
