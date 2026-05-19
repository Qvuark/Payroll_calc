namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Результат парсингу Excel-файлу. Містить кількість імпортованих,
/// оновлених та пропущених записів, а також список помилок, якщо вони виникли.
/// </summary>
public record ParserResult
{
    public int Imported { get; set; }
    public int Updated { get; set; }
    public int Skipped { get; set; }
    public List<ParserError> Errors { get; set; } = new();
}