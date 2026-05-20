using System.ComponentModel.DataAnnotations;

namespace PayrollCalc.Core.DTOs.Employee.Requests;

/// <summary>
/// Запит на створення нового працівника. Persona-only.
/// Ставки додаються окремо через POST /api/employees/{id}/positions.
/// </summary>
public class CreateEmployeeRequest
{
    [Required, MaxLength(50)]
    public string TabNumber { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// ІПН — 10 цифр. Друкується на розрахунковому листі.
    /// </summary>
    [MaxLength(10)]
    public string? TaxId { get; set; }
    /// <summary>
    /// Дата прийняття у школу (загальна, не на конкретну ставку).
    /// </summary>
    [Required]
    public DateOnly HireDate { get; set; }
    [MaxLength(200)]
    public string? Education { get; set; }
    /// <summary>
    /// Загальний педагогічний стаж на старт розрахункового року.
    /// </summary>
    public int PedExperienceYears { get; set; } = 0;
    /// <summary>
    /// Відсоток податкової соц.пільги. Null якщо пільги немає.
    /// </summary>
    public decimal? SocialBenefitPct { get; set; }
    /// <summary>
    /// Надбавка "За складність/напруженість" (5% від кожної активної ставки).
    /// </summary>
    public bool HasComplexityBonus { get; set; } = false;
    public int? TitleTypeId { get; set; }
}
