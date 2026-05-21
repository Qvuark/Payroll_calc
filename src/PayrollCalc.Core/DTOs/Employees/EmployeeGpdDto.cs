using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employees;

/// <summary>
/// Блок ГПД (група продовженого дня) — окрема оплата за години роботи з дітьми
/// після уроків. Власний тарифний розряд, відмінний від основного розряду ставки.
/// </summary>
public class EmployeeGpdDto
{
    public int EmployeePositionId { get; set; }
    public int TariffGradeId { get; set; }
    public decimal GpdHours { get; set; }

    /// <summary>
    /// Маппінг entity → DTO. Plain field-by-field.
    /// </summary>
    /// <param name="e">Entity ГПД.</param>
    /// <returns>DTO для відповіді API.</returns>
    public static EmployeeGpdDto FromEntity(EmployeeGpd e)
    {
        return new EmployeeGpdDto()
        {
            EmployeePositionId = e.EmployeePositionId,
            TariffGradeId = e.TariffGradeId,
            GpdHours = e.GpdHours
        };
    }
}