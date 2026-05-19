namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Представляє помилку, виявлену під час парсингу Excel-файлу.
/// </summary>
public record ParserError(
    int Row,
    string? Field,
    string Message,
    ErrorSeverity Severity = ErrorSeverity.Error
);

/// <summary>
/// Рівень критичності помилки.
/// </summary>
public enum ErrorSeverity
{
    Error,
    Warning
}