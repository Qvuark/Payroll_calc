using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Employee.Requests;

/// <summary>
/// Запит на редагування персональних даних працівника.
/// HireDate і TabNumber не міняються (історичні факти). Посади/ставки — через окремі endpoints.
/// </summary>
public class UpdateEmployeeRequest
{
    [Required, MaxLength(200)] public string FullName { get; set; } = string.Empty;
    [MaxLength(10)] public string? TaxId { get; set; }
    /// <summary>
    /// Заповнюється коли працівник звільняється з усіх ставок (повне звільнення).
    /// </summary>
    public DateOnly? DismissalDate { get; set; }
    [MaxLength(200)]public string? Education { get; set; }
    public int PedExperienceYears { get; set; } = 0;
    public decimal? SocialBenefitPct { get; set; }
    public bool HasComplexityBonus { get; set; } = false;
    public int? TitleTypeId { get; set; }
    [Required]
    public EmployeeStatus Status { get; set; }
}
