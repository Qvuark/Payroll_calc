namespace PayrollCalc.Core.Entities;

/// <summary>
/// Педагогічно-керівнича робота (ПКР). Окрема оплата за години керівництва
/// гуртком/секцією. Має власний тарифний розряд.
/// </summary>
public class EmployeePkr
{
    public int EmployeePositionId { get; set; }
    public EmployeePosition? EmployeePosition { get; set; }
    /// <summary>
    /// Тарифний розряд саме для ПКР.
    /// </summary>
    public int TariffGradeId { get; set; }
    public TariffGrade? TariffGrade { get; set; }
    /// <summary>
    /// Кількість годин ПКР на тиждень.
    /// </summary>
    public decimal PkrHours { get; set; } = decimal.Zero;
}
