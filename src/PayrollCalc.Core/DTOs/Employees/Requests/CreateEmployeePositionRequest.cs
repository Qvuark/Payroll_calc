using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Employees.Requests;

/// <summary>
/// Запит на додавання нової ставки працівнику. EmployeeId береться з URL.
/// </summary>
public class CreateEmployeePositionRequest
{
    [Required]
    public int PositionId { get; set; }
    [Required]
    public int TariffGradeId { get; set; }
    /// <summary>
    /// Кількість ставок: 0.5 = півставки, 1.0 = ставка, 1.5 = ставка з половиною.
    /// </summary>
    [Required, Range(0.01, 3.0)]
    public decimal RateCount { get; set; }
    /// <summary>
    /// Дата прийняття на цю конкретну посаду (не у школу — це Employee.HireDate).
    /// </summary>
    [Required]
    public DateOnly HireDate { get; set; }
    /// <summary>
    /// Головна ставка працівника (відображається у списках та на розрахунковому листі).
    /// Серед активних ставок одного працівника primary має бути рівно один.
    /// </summary>
    public bool IsPrimary { get; set; } = false;
    /// <summary>
    /// Перебуває на військовому обліку (надбавка 5%).
    /// </summary>
    public bool HasMilitaryRecord { get; set; } = false;
    /// <summary>
    /// Шкідливі умови праці (постанова КМУ №1298). Конкретний відсоток уточнюється з бухгалтером.
    /// </summary>
    public bool HasUnfavorable { get; set; } = false;
    /// <summary>
    /// Ставка надбавки за складність/напруженість роботи.
    /// </summary>
    [Range(0.05, 0.5)] public decimal? ComplexityBonusPct { get; set; }
    /// <summary>
    /// Ставка надбавки за престижність праці.
    /// </summary>
    [Range(0.05, 0.2)] public decimal? PrestigeBonusPct { get; set; }
    /// <summary>
    /// Дата початку роботи на цій посаді. Якщо не задано — використовується Employee.HireDate.
    /// </summary>
    public DateOnly? PositionStartDate { get; set; }
    /// <summary>
    /// Звання на цій ставці (per-position). Null = без звання. WorkerClass звання має збігатися з посадою.
    /// </summary>
    public int? TitleTypeId { get; set; }
    /// <summary>
    /// Маппінг Request → entity. EmployeeId передається окремо бо він приходить з URL.
    /// </summary>
    /// <param name="r">Дані запиту.</param>
    /// <param name="employeeId">Id працівника якому додається ставка.</param>
    /// <returns>Новий EmployeePosition, готовий до Add у DbContext.</returns>
    public static EmployeePosition FromRequest(CreateEmployeePositionRequest r, int employeeId)
    {
        return new EmployeePosition
        {
            EmployeeId = employeeId,
            PositionId = r.PositionId,
            TariffGradeId = r.TariffGradeId,
            RateCount = r.RateCount,
            HireDate = r.HireDate,
            PositionStartDate = r.PositionStartDate,
            EffectiveFrom = r.PositionStartDate ?? r.HireDate,
            IsPrimary = r.IsPrimary,
            HasMilitaryRecord = r.HasMilitaryRecord,
            HasUnfavorable = r.HasUnfavorable,
            ComplexityBonusPct = r.ComplexityBonusPct,
            PrestigeBonusPct = r.PrestigeBonusPct,
            TitleTypeId = r.TitleTypeId
        };
    }
}
