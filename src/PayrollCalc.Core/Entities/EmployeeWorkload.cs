namespace PayrollCalc.Core.Entities;

/// <summary>
/// Навантаження вчителя на ставці: години по класах (1-4, 5-9, 10-11),
/// індивідуальні, перевірка зошитів, інклюзивні. Тільки для Class 1 (вчителі).
/// </summary>
public class EmployeeWorkload
{
    public int EmployeePositionId { get; set; }
    public EmployeePosition? EmployeePosition { get; set; }
    public decimal Hours1To4 { get; set; } = decimal.Zero;
    public decimal IndividualHours1To4 { get; set; } = decimal.Zero;
    public decimal Hours5To9 { get; set; } = decimal.Zero;
    public decimal IndividualHours5To9 { get; set; } = decimal.Zero;
    public decimal Hours10To11 { get; set; } = decimal.Zero;
    public decimal IndividualHours10To11 { get; set; } = decimal.Zero;
    public decimal NotebookHours1To4 { get; set; } = decimal.Zero;
    public decimal NotebookHours5To9 { get; set; } = decimal.Zero;
    public decimal NotebookHours10To11 { get; set; } = decimal.Zero;
    public decimal InclusiveHours1To4 { get; set; } = decimal.Zero;
    public decimal InclusiveHours5To9 { get; set; } = decimal.Zero;
    public decimal InclusiveHours10To11 { get; set; } = decimal.Zero;
    /// <summary>
    /// FK на NotebookRate (мапа Subject → Pct: 10/15/20%). Визначається предметом вчителя.
    /// </summary>
    public int? NotebookRateId { get; set; }
    public NotebookRate? NotebookRate { get; set; }
}
