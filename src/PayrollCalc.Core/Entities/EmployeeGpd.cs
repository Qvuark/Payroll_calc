namespace PayrollCalc.Core.Entities;

/// <summary>
/// Група продовженого дня (ГПД). Окрема оплата за години роботи з дітьми
/// після уроків. Має власний тарифний розряд (відрізняється від основного розряду ставки).
/// </summary>
public class EmployeeGpd
{
    public int EmployeePositionId { get; set; }
    public EmployeePosition? EmployeePosition { get; set; }
    /// <summary>
    /// Тарифний розряд саме для ГПД.
    /// </summary>
    public int TariffGradeId { get; set; }
    public TariffGrade? TariffGrade { get; set; }
    /// <summary>
    /// Кількість СТАВОК ГПД (0.5 / 1.0), не годин: оплата = оклад розряду × це число.
    /// </summary>
    public decimal GpdRate { get; set; } = decimal.Zero;
}
