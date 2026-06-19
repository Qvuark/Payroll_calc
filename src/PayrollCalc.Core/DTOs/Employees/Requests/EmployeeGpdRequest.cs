using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Employees.Requests;

/// <summary>
/// Блок ГПД (Група продовженого дня). Окрема оплата за роботу з дітьми після уроків.
/// Має власний тарифний розряд, відмінний від основного розряду ставки.
/// </summary>
public class EmployeeGpdRequest
{
    /// <summary>
    /// Кількість ставок ГПД (0.5 / 1) — оплата = оклад розряду × це число, не години.
    /// </summary>
    [Range(0.0, 2.0)] public decimal GpdRate { get; set; }
    /// <summary>
    /// FK на TariffGrade — тарифний розряд саме для ГПД.
    /// </summary>
    [Range(1, int.MaxValue)] public int TariffGradeId { get; set; }

    /// <summary>
    /// Маппінг Request → entity. EmployeePositionId виставляє контролер.
    /// </summary>
    /// <param name="request">Дані запиту.</param>
    /// <returns>Новий EmployeeGpd, готовий до Add у DbContext.</returns>
    public static EmployeeGpd FromRequest(EmployeeGpdRequest request)
    {
        return new EmployeeGpd
        {
            GpdRate = request.GpdRate,
            TariffGradeId = request.TariffGradeId
        };
    }
}
