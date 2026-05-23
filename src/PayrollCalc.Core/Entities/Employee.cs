using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

/// <summary>
/// Працівник школи. Persona-level дані (ПІБ, ІПН, стаж, звання, статус).
/// Кожен працівник має одну або більше ставок (EmployeePosition) — на них
/// висять тарифний розряд, навантаження та блоки надбавок.
/// </summary>
public class Employee
{
    public int Id { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// ІПН — 10 цифр. Друкується на розрахунковому листі.
    /// </summary>
    public string TaxId { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; } = DateOnly.MinValue;
    public DateOnly? DismissalDate { get; set; }
    public string? Education { get; set; }
    /// <summary>
    /// Загальний педагогічний стаж у роках на початок розрахункового року.
    /// Використовується для derive TenurePct (надбавка за вислугу).
    /// Застосовується тільки до ставок Class 1.
    /// </summary>
    public int PedExperienceYears { get; set; } = 0;
    /// <summary>
    /// Загальний стаж роботи у роках на початок розрахункового року.
    /// Використовується для derive SickLeaveBase (лікарняні).
    /// </summary>
    public int GeneralExperienceYears { get; set; } = 0;
    /// <summary>
    /// Відсоток податкової соц.пільги. Впливає на базу ПДФО.
    /// Null якщо пільги немає. Вводиться вручну.
    /// </summary>
    public decimal? SocialBenefitPct { get; set; }
    public EmployeeStatus Status { get; set; }
    /// <summary>
    /// Звання працівника (per-person). Застосовується до ставок Class 1 та 2,
    /// які юридично приймають title-надбавку. Class 3 та 4 ігнорують навіть якщо запис є.
    /// </summary>
    public int? TitleTypeId { get; set; }
    public TitleType? TitleType { get; set; }
    /// <summary>
    /// Усі ставки працівника (директор + вчитель + ...). Несуть тарифний розряд,
    /// навантаження та per-position блоки надбавок.
    /// </summary>
    public ICollection<EmployeePosition> Positions { get; set; } = [];
}
