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
    /// Застосовується до педагогічних ставок (Class 1 і Class 2).
    /// </summary>
    public int PedExperienceYears { get; set; } = 0;
    /// <summary>
    /// Загальний стаж роботи у роках на початок розрахункового року.
    /// Використовується для виводу SickLeaveBase (лікарняні).
    /// </summary>
    public int GeneralExperienceYears { get; set; } = 0;
    /// <summary>
    /// Коефіцієнт податкової соц.пільги (частка: 1.0 = базова 100%, 1.5, 2.0). Null якщо пільги немає.
    /// Зменшує базу ПДФО на SocialBenefitLivingMin×коефіцієнт, але лише коли місячний дохід ≤ стелі
    /// (SocialBenefitCap). Стеля нижча за МЗП, тож на практиці спрацьовує лише в неповний місяць.
    /// </summary>
    public decimal? SocialBenefitPct { get; set; }
    public EmployeeStatus Status { get; set; }
    /// <summary>
    /// Прапор "Заслужений вчитель/працівник освіти" (звання президентського указу).
    /// Additive до TitleType — людина може бути одночасно методистом і заслуженим.
    /// </summary>
    public bool IsHonored { get; set; }
    /// <summary>
    /// Абсолютна сума надбавки за звання "Заслужений" (не %).
    /// Мама зберігає у тарифікації фіксовану суму, бо база не завжди = оклад.
    /// Застосовується тільки для Class 1 і Class 2. Null якщо IsHonored=false.
    /// </summary>
    public decimal? HonoredAmount { get; set; }
    /// <summary>
    /// Член профспілки. Якщо так — утримується внесок 1% (база gross мінус лікарняні ПФУ).
    /// Не член → внесок 0. За замовчуванням true (більшість працівників — члени).
    /// </summary>
    public bool IsUnionMember { get; set; } = true;
    /// <summary>
    /// Усі ставки працівника (директор + вчитель + ...). Несуть тарифний розряд,
    /// навантаження та per-position блоки надбавок.
    /// </summary>
    public ICollection<EmployeePosition> Positions { get; set; } = [];
}
