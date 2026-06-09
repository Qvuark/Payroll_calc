using System.Globalization;
namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Допоміжний клас для парсингу десяткових чисел з різних типів даних.
/// </summary>
public static class DecimalParser
{
    /// <summary>
    /// Парсить значення у десяткове число з округленням до 2 знаків після коми.
    /// </summary>
    /// <param name="value">Значення для парсингу (може бути int, double, decimal, string або null).</param>
    /// <param name="result">Розпарсений результат.</param>
    /// <returns>true, якщо парсинг успішний, false в іншому випадку.</returns>
    /// <summary>
    /// Культура бухгалтера (кома-decimal). Жорстко uk-UA, НЕ CurrentCulture —
    /// інакше парсинг "3,14" залежав би від мови Windows конкретної машини.
    /// </summary>
    private static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("uk-UA");

    public static bool TryParse(object? value, out decimal result)
    {
        // out обов'язково присвоїти до return — інакше компілятор лається
        result = 0m;
        if(value is null || value is DBNull)
            return false;
        // ExcelDataReader повертає DBNull для пустих ячейок, не null. Обидві
        // перевірки потрібні.
        if(value is double d)
        {
            // Excel-ячейка може нести 1E300 чи NaN — пряме (decimal)d кинуло б
            // OverflowException і завалило весь імпорт замість помилки рядка.
            if (double.IsNaN(d) || double.IsInfinity(d) || Math.Abs(d) > (double)decimal.MaxValue)
                return false;
            // Бухгалтерське округлення: 2.5 → 3 (НЕ banker's ToEven 2.5 → 2).
            // Дефолтний ToEven дав би розходження копійок у масових
            // розрахунках.
            result = Math.Round((decimal)d, 2, MidpointRounding.AwayFromZero);
            return true;
        }
        if(value is decimal dec)
        {
            result = Math.Round(dec, 2, MidpointRounding.AwayFromZero);
            return true;
        }
        if(value is int i)
        {
            result = i;
            return true;
        }
        var str = value.ToString();
        if(string.IsNullOrWhiteSpace(str))
            return false;
        // Float — тільки знак + крапка/кома як decimal, БЕЗ thousand separator.
        // Інакше "3,14" на Invariant парситься як 314 (кома = розділювач тисяч).
        // Спочатку Invariant (крапка-decimal), потім uk-UA (кома-decimal).
        if(decimal.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            || decimal.TryParse(str, NumberStyles.Float, UkCulture, out parsed))
        {
            result = Math.Round(parsed, 2, MidpointRounding.AwayFromZero);
            return true;
        }
        return false;
    }
}