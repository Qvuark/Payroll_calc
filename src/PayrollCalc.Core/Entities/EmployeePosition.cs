namespace PayrollCalc.Core.Entities;

/// <summary>
/// Ставка працівника на конкретній посаді. Один працівник може мати N ставок
/// (наприклад директор + вчитель математики). Несе тарифний розряд, кількість ставок
/// та блоки навантаження/адмін/ГПД/ПКР/непедагогічні.
/// </summary>
public class EmployeePosition
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int PositionId { get; set; }
    public Position? Position { get; set; }
    public int TariffGradeId { get; set; }
    public TariffGrade? TariffGrade { get; set; }
    /// <summary>
    /// Кількість ставок цієї посади (0.5 = півставки, 1.0 = ставка, 1.5 = півтори).
    /// </summary>
    public decimal RateCount { get; set; }
    /// <summary>
    /// Головна ставка працівника. Використовується для відображення у списках та на розрахунковому листі.
    /// </summary>
    public bool IsPrimary { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly? DismissalDate { get; set; }
    /// <summary>
    /// Дата з якої запис чинний. У MVP дорівнює HireDate; у Phase 2.7 використовується для versioning історії змін ставки.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }
    /// <summary>
    /// Чи перебуває на військовому обліку на цій ставці (надбавка 5% за наказом).
    /// </summary>
    public bool HasMilitaryRecord { get; set; }
    /// <summary>
    /// Чи є шкідливі умови праці на цій ставці.
    /// Формула/відсоток відкладено до уточнення у бухгалтера.
    /// </summary>
    public bool HasUnfavorable { get; set; }
    public EmployeeWorkload? Workload { get; set; }
    public EmployeeAdmin? Admin { get; set; }
    public EmployeeGpd? Gpd { get; set; }
    public EmployeePkr? Pkr { get; set; }
    public EmployeeNonPedagogical? NonPedagogical { get; set; }
}
