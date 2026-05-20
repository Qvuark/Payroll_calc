using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Employee.Requests;

/// <summary>
/// Блок ПКР (Педагогічно-керівнича робота). Окрема оплата за керівництво гуртком/секцією.
/// Має власний тарифний розряд.
/// </summary>
public class EmployeePkrRequest
{
    /// <summary>
    /// Кількість годин ПКР на тиждень.
    /// </summary>
    [Range(0.0, 40.0)] public decimal PkrHours { get; set; }
    /// <summary>
    /// FK на TariffGrade — тарифний розряд саме для ПКР.
    /// </summary>
    [Range(1, int.MaxValue)] public int TariffGradeId { get; set; }

    /// <summary>
    /// Маппінг Request → entity. EmployeePositionId виставляє контролер.
    /// </summary>
    /// <param name="request">Дані запиту.</param>
    /// <returns>Новий EmployeePkr, готовий до Add у DbContext.</returns>
    public static EmployeePkr FromRequest(EmployeePkrRequest request)
    {
        return new EmployeePkr
        {
            PkrHours = request.PkrHours,
            TariffGradeId = request.TariffGradeId
        };
    }
}
