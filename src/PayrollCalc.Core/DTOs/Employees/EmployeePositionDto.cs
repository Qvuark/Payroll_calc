using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Employees;

/// <summary>
/// Одна ставка працівника. Включає інформацію про посаду, тариф,
/// метадані ставки (RateCount, primary, dates) та nullable блоки навантаження/доплат.
/// </summary>
public class EmployeePositionDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public int PositionId { get; set; }
    public string PositionName { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public WorkerClass WorkerClass { get; set; }
    public int TariffGradeId { get; set; }
    /// <summary>
    /// Номер тарифного розряду (1-25). Не FK Id, а саме номер з закону.
    /// </summary>
    public int TariffGrade { get; set; }
    /// <summary>
    /// Місячний оклад тарифного розряду (з TariffGrade.MonthlyRate).
    /// </summary>
    public decimal TariffMonthlyRate { get; set; } = 0;
    /// <summary>
    /// Кількість ставок: 0.5 = півставки, 1.0 = ставка, 1.5 = ставка з половиною.
    /// </summary>
    public decimal RateCount { get; set; }
    /// <summary>
    /// Головна ставка працівника. Використовується для відображення у списках і на розрахунковому листі.
    /// </summary>
    public bool IsPrimary { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly? DismissalDate { get; set; }
    /// <summary>
    /// Дата початку роботи на цій посаді (вводиться окремо). Null → дорівнює HireDate.
    /// </summary>
    public DateOnly? PositionStartDate { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    /// <summary>
    /// Перебуває на військовому обліку на цій ставці (надбавка 5%).
    /// </summary>
    public bool HasMilitaryRecord { get; set; }
    /// <summary>
    /// Шкідливі умови праці на цій ставці. Формула/відсоток уточнюється з бухгалтером.
    /// </summary>
    public bool HasUnfavorable { get; set; }
    ///<summary>
    /// Відсоток надбавки за складність/напруженість роботи.
    /// </summary>
    public decimal? ComplexityBonusPct { get; set; }
    /// <summary>
    /// Відсоток надбавки за престижність праці.
    /// </summary>
    public decimal? PrestigeBonusPct { get; set; }
    /// <summary>
    /// Звання на цій ставці (per-position). Null якщо без звання.
    /// </summary>
    public int? TitleTypeId { get; set; }
    public string? TitleTypeName { get; set; }
    public EmployeeWorkloadDto? Workload { get; set; }
    public EmployeeAdminDto? Admin { get; set; }
    public EmployeeGpdDto? Gpd { get; set; }
    public EmployeePkrDto? Pkr { get; set; }
    public EmployeeNonPedagogicalDto? NonPedagogical { get; set; }

    /// <summary>
    /// Маппінг entity → DTO. Потребує Include(Position).ThenInclude(Department)
    /// та Include(TariffGrade) у запиті, інакше read-only поля будуть порожніми.
    /// </summary>
    /// <param name="e">Entity ставки з завантаженими навігаціями.</param>
    /// <returns>DTO зі всіма заповненими полями.</returns>
    public static EmployeePositionDto FromEntity(EmployeePosition e)
    {
        return new EmployeePositionDto
        {
            Id = e.Id,
            EmployeeId = e.EmployeeId,
            PositionId = e.PositionId,
            PositionName = e.Position?.Name ?? "",
            DepartmentName = e.Position?.Department?.Name ?? "",
            WorkerClass = e.Position?.WorkerClass ?? WorkerClass.Pedagogical,
            TariffGradeId = e.TariffGradeId,
            TariffGrade = e.TariffGrade?.Grade ?? 0,
            TariffMonthlyRate = e.TariffGrade?.MonthlyRate ?? 0,
            RateCount = e.RateCount,
            IsPrimary = e.IsPrimary,
            HireDate = e.HireDate,
            DismissalDate = e.DismissalDate,
            PositionStartDate = e.PositionStartDate,
            EffectiveFrom = e.EffectiveFrom,
            HasMilitaryRecord = e.HasMilitaryRecord,
            HasUnfavorable = e.HasUnfavorable,
            ComplexityBonusPct = e.ComplexityBonusPct,
            PrestigeBonusPct = e.PrestigeBonusPct,
            TitleTypeId = e.TitleTypeId,
            TitleTypeName = e.TitleType?.Name,
            Workload = e.Workload != null ? EmployeeWorkloadDto.FromEntity(e.Workload) : null,
            Admin = e.Admin != null ? EmployeeAdminDto.FromEntity(e.Admin) : null,
            Gpd = e.Gpd != null ? EmployeeGpdDto.FromEntity(e.Gpd) : null,
            Pkr = e.Pkr != null ? EmployeePkrDto.FromEntity(e.Pkr) : null,
            NonPedagogical = e.NonPedagogical != null ? EmployeeNonPedagogicalDto.FromEntity(e.NonPedagogical) : null
        };
    }
}
