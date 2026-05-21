using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Employees;

/// <summary>
/// Адмін-блок ставки (керівні відсотки + надбавки за класне керівництво,
/// завідування кабінетом/спортзалом/тиром, сайт, позакласну роботу).
/// Дозволено тільки для Class 2 (AdminPedagogical).
/// </summary>
public class EmployeeAdminDto
{
    public int EmployeePositionId { get; set; }
    public decimal DirectorPct { get; set; } = decimal.Zero;
    public decimal AdminRateCount { get; set; } = decimal.Zero;
    public decimal PedRateCount { get; set; } = decimal.Zero;
    public bool HasClassMgmt { get; set; } = false;
    public ClassGradeGroup? ClassGradeGroup { get; set; }
    public bool HasCabinet { get; set; } = false;
    public CabinetType? CabinetType { get; set; }
    public bool HasGym { get; set; } = false;
    public bool HasShootingRange { get; set; } = false;
    public bool HasComputers { get; set; } = false;
    public bool HasExtracurricular { get; set; } = false;
    public bool HasWebsite { get; set; } = false;

    /// <summary>
    /// Маппінг entity → DTO. Plain field-by-field, без обчислень.
    /// </summary>
    /// <param name="e">Entity адмін-блока.</param>
    /// <returns>DTO для відповіді API.</returns>
    public static EmployeeAdminDto FromEntity(EmployeeAdmin e)
    {
        return new EmployeeAdminDto()
        {
            EmployeePositionId = e.EmployeePositionId,
            DirectorPct = e.DirectorPct,
            AdminRateCount = e.AdminRateCount,
            PedRateCount = e.PedRateCount,
            HasClassMgmt = e.HasClassMgmt,
            ClassGradeGroup = e.ClassGradeGroup,
            HasCabinet = e.HasCabinet,
            CabinetType = e.CabinetType,
            HasGym = e.HasGym,
            HasShootingRange = e.HasShootingRange,
            HasComputers = e.HasComputers,
            HasExtracurricular = e.HasExtracurricular,
            HasWebsite = e.HasWebsite
        };
    }
}