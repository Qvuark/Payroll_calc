using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employees;

/// <summary>
/// Блок ПКР (педагогічно-керівнича робота) — оплата за керівництво гуртком/секцією.
/// Власний тарифний розряд, відмінний від основного розряду ставки.
/// </summary>
public class EmployeePkrDto
{
    public int EmployeePositionId { get; set; }
    public int TariffGradeId { get; set; }
    public decimal PkrHours { get; set; }

    /// <summary>
    /// Маппінг entity → DTO. Plain field-by-field.
    /// </summary>
    /// <param name="e">Entity ПКР.</param>
    /// <returns>DTO для відповіді API.</returns>
    public static EmployeePkrDto FromEntity(EmployeePkr e)
    {
        return new EmployeePkrDto()
        {
            EmployeePositionId = e.EmployeePositionId,
            TariffGradeId = e.TariffGradeId,
            PkrHours = e.PkrHours
        };
    }
}