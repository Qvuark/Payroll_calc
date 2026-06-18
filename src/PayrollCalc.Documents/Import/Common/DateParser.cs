using System.Globalization;

namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Парсер дат з Excel-ячейок. Розуміє DateTime, Excel serial number (double)
/// і текстові формати укр./ISO. Повертає DateOnly — час нам не потрібен
/// (дата народження, дата найму).
/// </summary>
public static class DateParser
{
    // TryParseExact приймає масив форматів і йде по черзі. Розширюй сюди коли
    // бухгалтер принесе файл у новому форматі — без правки логіки.
    private static readonly string[] Formats = new[]
    {
        "dd.MM.yyyy",   // 12.05.2024 — укр./рос. стандарт
        "yyyy-MM-dd",   // 2024-05-12 — ISO
        "dd/MM/yyyy",   // 12/05/2024
        "d.M.yyyy",     // 1.5.2024 — без ведучих нулів
        "dd.MM.yy"      // 12.05.24 — короткий рік
    };
    /// <summary>
    /// Намагається перетворити значення з Excel-ячейки на DateOnly.
    /// </summary>
    /// <param name="value">Сире значення з ячейки.</param>
    /// <param name="result">Результат парсингу. Має сенс лише коли метод повернув true.</param>
    /// <returns>true — дату розпізнано. false — невідома форма, треба ParserError.</returns>
    public static bool TryParse(object? value, out DateOnly result)
    {
        result = default;
        if (value is null || value is DBNull)
            return false;
        // Ячейка з типом "Date" в Excel — ExcelDataReader віддає DateTime напряму.
        if (value is DateTime dt)
        {
            result = DateOnly.FromDateTime(dt);
            return true;
        }
        // OADate = OLE Automation Date = серійний номер дня від 1900-01-01.
        // Excel зберігає дати так під капотом, якщо ячейка без явного формату.
        if (value is double d)
        {
            // Діапазон валідних OADate: поза ним FromOADate кидає ArgumentException
            // і валив би весь імпорт замість помилки рядка (напр. число 9999999 у колонці дати).
            if (d is < -657435.0 or >= 2958466.0)
                return false;
            result = DateOnly.FromDateTime(DateTime.FromOADate(d));
            return true;
        }
        var s = value.ToString()?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(s))
            return false;
        // TryParseExact (не TryParse): фіксований список форматів +
        // InvariantCulture. Інакше "12.05.2024" на американській машині
        // спарситься як 5 грудня 2024 (mm.dd.yyyy). Тут це боляче.
        if (DateTime.TryParseExact(s, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            result = DateOnly.FromDateTime(parsed);
            return true;
        }
        return false;
    }
}
