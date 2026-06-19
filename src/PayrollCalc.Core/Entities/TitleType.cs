using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Interfaces;

namespace PayrollCalc.Core.Entities;

/// <summary>
/// Звання працівника (посада з надбавкою: методист, старший учитель тощо).
/// </summary>
public class TitleType : IAliasable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// До якої категорії відноситься звання.
    /// </summary>
    public WorkerClass WorkerClass { get; set; }
    /// <summary>
    /// Відсоток надбавки за звання. Використовується для derive BonusAmount в разі якщо звання встановлено.
    /// </summary>
    public decimal Pct { get; set; } = decimal.Zero;
    /// <summary>
    /// Скорочення з Excel-файлів які мапляться на це звання (jsonb-колонка у PG).
    /// Приклад: для "Старший вчитель" — ["ст.вчитель", "ст. вчитель", "старший вч."].
    /// Той самий pattern що Position.ExcelAliases.
    /// </summary>
    public List<string> ExcelAliases { get; set; } = [];
}