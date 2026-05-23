using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

/// <summary>
/// Звання працівника (посада з надбавкою: методист, старший учитель тощо).
/// </summary>
public class TitleType
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
}