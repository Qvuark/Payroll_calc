using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities.Enums;
using Employee = PayrollCalc.Core.Entities.Employee;

namespace PayrollCalc.Core.DTOs.Employees.Requests;

/// <summary>
/// Запит на створення нового працівника. Persona-only.
/// Ставки додаються окремо через POST /api/employees/{id}/positions.
/// </summary>
public class CreateEmployeeRequest
{
    [Required, MaxLength(50)] public string TabNumber { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// ІПН — 10 цифр. Друкується на розрахунковому листі.
    /// </summary>
    [Required, StringLength(10, MinimumLength = 10)] public string TaxId { get; set; } = string.Empty;
    /// <summary>
    /// Дата прийняття у школу (загальна, не на конкретну ставку).
    /// </summary>
    [Required] public DateOnly HireDate { get; set; }
    [MaxLength(200)] public string? Education { get; set; }
    /// <summary>
    /// Загальний стаж роботи у роках на початок розрахункового року.
    /// </summary>
    public int GeneralExperienceYears { get; set; } = 0;
    /// <summary>
    /// Загальний педагогічний стаж на старт розрахункового року.
    /// </summary>
    public int PedExperienceYears { get; set; } = 0;
    /// <summary>
    /// Відсоток податкової соц.пільги. Null якщо пільги немає.
    /// </summary>
    public decimal? SocialBenefitPct { get; set; }
    public int? TitleTypeId { get; set; }

    /// <summary>
    /// Маппінг Request → entity Employee. Status за замовчуванням Active —
    /// зміна статусу можлива тільки через PUT (server-controlled state).
    /// </summary>
    /// <param name="r">Дані запиту.</param>
    /// <returns>Новий Employee зі Status=Active, готовий до Add у DbContext.</returns>
    public static Employee FromRequest(CreateEmployeeRequest r)
    {
        return new Employee
        {
            TabNumber = r.TabNumber,
            FullName = r.FullName,
            TaxId = r.TaxId,
            HireDate = r.HireDate,
            Education = r.Education,
            PedExperienceYears = r.PedExperienceYears,
            GeneralExperienceYears = r.GeneralExperienceYears,
            SocialBenefitPct = r.SocialBenefitPct,
            TitleTypeId = r.TitleTypeId,
            Status = EmployeeStatus.Active
        };
    }
}
