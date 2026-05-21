using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Employees.Requests;

/// <summary>
/// Блок адмін-надбавок ставки. Несе керівні відсотки (директор/заступник) та надбавки
/// які прив'язані до конкретної посади: класне керівництво, завідування кабінетом,
/// спортзалом, тиром, комп'ютерним класом, позакласна робота, ведення сайту.
/// На відміну від EmployeeNonPedagogical — тут усе у відсотках, не у фіксованих сумах.
/// </summary>
public class EmployeeAdminRequest
{
    /// <summary>
    /// Відсоток директорства від окладу (1.0 = 100%). Тільки для директора.
    /// </summary>
    [Range(0.0, 1.0)] public decimal DirectorPct { get; set; }
    /// <summary>
    /// Кількість адмін-ставок (для заступника директора).
    /// </summary>
    [Range(0.0, 2.0)] public decimal AdminRateCount { get; set; }
    /// <summary>
    /// Кількість педагогічних ставок (для заступника, який паралельно викладає).
    /// </summary>
    [Range(0.0, 2.0)] public decimal PedRateCount { get; set; }
    public bool HasClassMgmt { get; set; } = false;
    /// <summary>
    /// Група класів для розрахунку класного керівництва (1-4 чи 5-11).
    /// Має сенс лише якщо HasClassMgmt = true.
    /// </summary>
    public ClassGradeGroup? ClassGradeGroup { get; set; }
    public bool HasCabinet { get; set; } = false;
    /// <summary>
    /// Тип кабінету (звичайний / музика-ІТ / майстерня) — впливає на відсоток надбавки.
    /// Має сенс лише якщо HasCabinet = true.
    /// </summary>
    public CabinetType? CabinetType { get; set; }
    public bool HasGym { get; set; } = false;
    public bool HasShootingRange { get; set; } = false;
    public bool HasComputers { get; set; } = false;
    public bool HasExtracurricular { get; set; } = false;
    public bool HasWebsite { get; set; } = false;

    /// <summary>
    /// Маппінг Request → entity. EmployeePositionId виставляє контролер.
    /// </summary>
    /// <param name="request">Дані запиту.</param>
    /// <returns>Новий EmployeeAdmin, готовий до Add у DbContext.</returns>
    public static EmployeeAdmin FromRequest(EmployeeAdminRequest request)
    {
        return new EmployeeAdmin
        {
            DirectorPct = request.DirectorPct,
            AdminRateCount = request.AdminRateCount,
            PedRateCount = request.PedRateCount,
            HasClassMgmt = request.HasClassMgmt,
            HasCabinet = request.HasCabinet,
            ClassGradeGroup = request.ClassGradeGroup,
            CabinetType = request.CabinetType,
            HasGym = request.HasGym,
            HasShootingRange = request.HasShootingRange,
            HasComputers = request.HasComputers,
            HasExtracurricular = request.HasExtracurricular,
            HasWebsite = request.HasWebsite
        };
    }
}
