using PayrollCalc.Core.Entities.Enums;
using EmployeeEntity = PayrollCalc.Core.Entities.Employee;

namespace PayrollCalc.Core.DTOs.Employees;

/// <summary>
/// Повна картка працівника (GET /api/employees/{id}).
/// Persona-поля + список усіх ставок з вкладеними блоками навантаження/доплат.
/// </summary>
public class EmployeeDetailDto
{
    public int Id { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    /// <summary>
    /// ІПН — 10 цифр. Друкується на розрахунковому листі.
    /// </summary>
    public string TaxId { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public DateOnly? DismissalDate { get; set; }
    public string? Education { get; set; }
    /// <summary>
    /// Загальний стаж роботи у роках на початок розрахункового року.
    /// </summary>
    public int GeneralExperienceYears { get; set; }
    /// <summary>
    /// Загальний педагогічний стаж на старт розрахункового року.
    /// </summary>
    public int PedExperienceYears { get; set; }
    public EmployeeStatus Status { get; set; }
    /// <summary>
    /// Відсоток податкової соц.пільги. Null якщо пільги немає.
    /// </summary>
    public decimal? SocialBenefitPct { get; set; }
    /// <summary>
    /// Заслужений вчитель/працівник освіти. Віддається у GET, бо PUT вимагає це поле —
    /// інакше клієнт не знає поточного значення і затирає прапор дефолтом.
    /// </summary>
    public bool IsHonored { get; set; }
    /// <summary>
    /// Сума надбавки за звання «Заслужений». Null коли IsHonored=false.
    /// </summary>
    public decimal? HonoredAmount { get; set; }
    /// <summary>
    /// Член профспілки — чи утримувати внесок 1%. Віддається у GET, бо PUT вимагає це поле.
    /// </summary>
    public bool IsUnionMember { get; set; }
    /// <summary>
    /// Усі ставки працівника (активні та звільнені). Кожна несе власні блоки.
    /// </summary>
    public List<EmployeePositionDto> Positions { get; set; } = [];

    /// <summary>
    /// Маппінг entity → DTO. Потребує Include на TitleType + Positions з усіма дочірніми
    /// (Position, Department, TariffGrade, Workload, Admin, Gpd, Pkr, NonPedagogical).
    /// </summary>
    /// <param name="e">Entity працівника з усіма завантаженими навігаціями.</param>
    /// <returns>Повний DTO для картки.</returns>
    public static EmployeeDetailDto FromEntity(EmployeeEntity e)
    {
        return new EmployeeDetailDto
        {
            Id = e.Id,
            TabNumber = e.TabNumber,
            FullName = e.FullName,
            TaxId = e.TaxId,
            HireDate = e.HireDate,
            DismissalDate = e.DismissalDate,
            Education = e.Education,
            GeneralExperienceYears = e.GeneralExperienceYears,
            PedExperienceYears = e.PedExperienceYears,
            Status = e.Status,
            SocialBenefitPct = e.SocialBenefitPct,
            IsHonored = e.IsHonored,
            HonoredAmount = e.HonoredAmount,
            IsUnionMember = e.IsUnionMember,
            Positions = e.Positions.Select(EmployeePositionDto.FromEntity).ToList()
        };
    }
}
