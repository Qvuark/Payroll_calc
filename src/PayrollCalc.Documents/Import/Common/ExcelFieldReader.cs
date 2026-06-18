using System.Data;

namespace PayrollCalc.Documents.Import.Common;

public static class ExcelFieldReader
{
    // ─── Mandatory helpers ───
    // Контракт: пусто АБО кривий формат → error + return null.
    // Caller перевіряє null у early-return, без mandatory DTO нема сенсу будувати.
    public static string? GetMandatoryString(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var val = row[col]?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(val))
        {
            errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' обов'язкове"));
            return null;
        }
        return val;
    }
    public static DateOnly? GetMandatoryDate(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        // row[col] віддаємо сирим — DateParser сам розбере DateTime/double/string,
        // ToString() з'їв би type info і ламав би Excel serial dates.
        if (DateParser.TryParse(row[col], out var date))
            return date;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' обов'язкове або має бути датою"));
        return null;
    }
    public static decimal? GetMandatoryDecimal(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        if (DecimalParser.TryParse(row[col], out var dec))
            return dec;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' обов'язкове або має бути числом"));
        return null;
    }
    public static int? GetMandatoryInt(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col]?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' обов'язкове"));
            return null;
        }
        if (int.TryParse(raw, out var i))
            return i;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути цілим числом"));
        return null;
    }
    // ─── Optional helpers ───
    // Різниця: пусто = null/default (без error), кривий формат = error + null/default.
    public static string? GetOptionalString(DataRow row, int col)
    {
        var val = row[col]?.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(val) ? null : val;
    }
    public static DateOnly? GetOptionalDate(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col];
        if (raw is null || raw is DBNull || string.IsNullOrWhiteSpace(raw.ToString()))
            return null;
        if (DateParser.TryParse(raw, out var d))
            return d;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути датою"));
        return null;
    }
    public static decimal? GetOptionalDecimal(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col];
        if (raw is null || raw is DBNull || string.IsNullOrWhiteSpace(raw.ToString()))
            return null;
        if (DecimalParser.TryParse(raw, out var dec))
            return dec;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути числом"));
        return null;
    }
    public static int? GetOptionalInt(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col]?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (int.TryParse(raw, out var i))
            return i;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути цілим числом"));
        return null;
    }
    public static bool GetOptionalBool(DataRow row, int col)
    {
        // BoolParser.TryParse повертає false на null/пусто/невідоме значення.
        // Для bool поля default false — ок, бухгалтер не позначив = не активно.
        _ = BoolParser.TryParse(row[col], out var b);
        return b;
    }
    /// <summary>
    /// Окремо від GetOptionalDecimal: повертає не <c>decimal?</c>, а <c>decimal</c>
    /// з дефолтом 0m. Hours-поля в DTO non-nullable, бо 0 годин = валідне значення
    /// (а не "не вказано"), і калькулятор передбачає decimal не decimal?.
    /// </summary>
    public static decimal GetOptionalHours(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col];
        if (raw is null || raw is DBNull || string.IsNullOrWhiteSpace(raw.ToString()))
            return 0m;
        if (DecimalParser.TryParse(raw, out var dec))
            return dec;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути числом"));
        return 0m;
    }
}