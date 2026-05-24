namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Парсер булевих значень з Excel-ячейок. Розуміє bool, числа і
/// текстові форми укр./англ. ("так"/"ні"/"yes"/"no"/"1"/"0" тощо).
/// </summary>
public static class BoolParser
{
    // HashSet + OrdinalIgnoreCase: O(1) пошук без чутливості до регістру.
    // Comparer впливає і на хеш, і на порівняння — "ТАК" знайдеться як "так".
    private static readonly HashSet<string> TrueValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "так", "да", "yes", "y", "true", "1", "+", "✓"
    };
    private static readonly HashSet<string> FalseValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "ні", "нет", "no", "n", "false", "0", "-", ""
    };

    /// <summary>
    /// Намагається перетворити значення з Excel-ячейки на bool.
    /// </summary>
    /// <param name="value">Сире значення з ячейки (object?, бо ExcelDataReader повертає різні типи).</param>
    /// <param name="result">Результат парсингу. Має сенс лише коли метод повернув true.</param>
    /// <returns>true — значення розпізнано (навіть якщо result=false). false — невідома форма, треба ParserError.</returns>
    public static bool TryParse(object? value, out bool result)
    {
        result = false;
        // ExcelDataReader повертає DBNull для пустих ячейок (не null). Обидва кейси перекриваємо.
        if (value is null || value is DBNull)
            return false;
        if (value is bool b)
        {
            result = b;
            return true;
        }
        // Числа: != 0 → true. Окремі if-и замість value is int or double — pattern matching
        // не дозволяє оголосити одну змінну у різних гілках ||.
        if (value is int i)
        {
            result = i != 0;
            return true;
        }
        if (value is decimal d)
        {
            result = d != 0m;
            return true;
        }
        if (value is double x)
        {
            result = x != 0d;
            return true;
        }
        var s = value.ToString()?.Trim() ?? string.Empty;
        if (TrueValues.Contains(s))
        {
            result = true;
            return true;
        }
        // Порожня ячейка = "галочку не поставили" = false. Бізнес-конвенція бухгалтерських форм.
        if (FalseValues.Contains(s))
        {
            result = false;
            return true;
        }
        return false;
    }
}
