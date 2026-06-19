using System.Text.RegularExpressions;
using PayrollCalc.Core.Interfaces;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Зіставляє назву з Excel із записом довідника (Position/TitleType), коли бухгалтер пише
/// скорочено чи інакше ("вч." замість "Вчитель"). Звіряє і канонічну назву, і синоніми
/// (ExcelAliases), пропускаючи обидва через Normalize — той гасить різницю в регістрі,
/// крапках і пробілах ("Вч. Математики" = "вч математики").
/// </summary>
public static class AliasMatcher
{
    // \s+ — група з одного й більше пробільних символів (пробіл, таб). Replace стискає будь-яку
    // таку групу в один пробіл: після заміни крапок на пробіли міг зʼявитися подвійний пробіл.
    // Compiled — шаблон компілимо один раз при старті, щоб кожен виклик не парсив рядок-патерн знову.
    private static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Зводить рядок до канонічної форми: trim, нижній регістр, крапки → пробіли, стиснуті пробіли.
    /// Крапку міняємо на пробіл (не на пусто), щоб "вч.математики" стало "вч математики", а не "вчматематики".
    /// </summary>
    /// <param name="value">Сирий рядок — назва з файлу або з довідника.</param>
    /// <returns>Нормалізована форма для порівняння на рівність.</returns>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var lowered = value.Trim().ToLowerInvariant().Replace(".", " ");
        return MultiSpace.Replace(lowered, " ").Trim();
    }

    /// <summary>
    /// Знаходить кандидатів, чий Name АБО будь-який alias дорівнює input після нормалізації.
    /// Повертає ВСІ збіги, щоб викликач відрізнив: 0 = не знайдено, 1 = ок, 2+ = неоднозначно.
    /// </summary>
    /// <param name="candidates">Довідник у пам'яті (малий — тягнемо весь, як NotebookRates).</param>
    /// <param name="input">Рядок з Excel-файлу. null/порожній → жодного збігу.</param>
    /// <returns>Список записів-збігів (зазвичай 0 або 1).</returns>
    public static List<T> Match<T>(IEnumerable<T> candidates, string? input) where T : IAliasable
    {
        var needle = Normalize(input);
        return candidates
            .Where(c => Normalize(c.Name) == needle || c.ExcelAliases.Any(a => Normalize(a) == needle))
            .ToList();
    }
}
