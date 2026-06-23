using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Employees.Requests;

/// <summary>
/// Запит на редагування персональних даних працівника.
/// HireDate і TabNumber не міняються (історичні факти). Посади/ставки — через окремі endpoints.
/// </summary>
public class UpdateEmployeeRequest
{
    [Required, MaxLength(200)] public string FullName { get; set; } = string.Empty;
    [Required, RegularExpression(@"^\d{10}$", ErrorMessage = "ІПН має містити рівно 10 цифр.")] public string TaxId { get; set; } = string.Empty;
    /// <summary>
    /// Заповнюється коли працівник звільняється з усіх ставок (повне звільнення).
    /// </summary>
    public DateOnly? DismissalDate { get; set; }
    [MaxLength(200)] public string? Education { get; set; }
    /// <summary>
    /// Загальний стаж роботи у роках на початок розрахункового року.
    /// </summary>
    public int GeneralExperienceYears { get; set; } = 0;
    /// <summary>
    /// Загальний педагогічний стаж на старт розрахункового року.
    /// </summary>
    public int PedExperienceYears { get; set; } = 0;
    public decimal? SocialBenefitPct { get; set; }
    [Required]
    public EmployeeStatus Status { get; set; }
    /// <summary>
    /// Заслужений вчитель/працівник освіти — фіксована надбавка.
    /// </summary>
    public bool IsHonored { get; set; }
    /// <summary>
    /// Сума надбавки за звання. Null коли IsHonored=false.
    /// </summary>
    public decimal? HonoredAmount { get; set; }
    /// <summary>
    /// Член профспілки — чи утримувати внесок 1%.
    /// </summary>
    public bool IsUnionMember { get; set; } = true;
}
