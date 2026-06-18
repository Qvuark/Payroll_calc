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
    /// Звання на цій ставці: scope = WorkerClass посади. Прив'язане до ставки, а не до людини,
    /// бо директор-вчитель має різні звання на різних посадах (вчитель-методист vs психолог-методист).
    /// </summary>
    public int? TitleTypeId { get; set; }
    public TitleType? TitleType { get; set; }
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
    /// Дата прийняття на цю конкретну посаду (не на школу). Парситься з xlsx-колонки "Дата на посаді".
    /// EffectiveFrom = PositionStartDate ?? HireDate (fallback на загальну дату прийому при null).
    /// </summary>
    public DateOnly? PositionStartDate { get; set; }
    /// <summary>
    /// Дата з якої запис чинний. У MVP дорівнює HireDate; закладено під майбутній versioning історії змін ставки.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }
    /// <summary>
    /// Дата закриття чинності запису для versioning. У MVP завжди null.
    /// Закладено під майбутнє зберігання історії змін ставки без втрати старих даних
    /// (не плутати з DismissalDate — це різні концепти: запис vs звільнення з посади).
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }
    /// <summary>
    /// Чи перебуває на військовому обліку на цій ставці (надбавка 5% за наказом).
    /// </summary>
    public bool HasMilitaryRecord { get; set; }
    /// <summary>
    /// Чи є шкідливі умови праці на цій ставці.
    /// Формула/відсоток відкладено до уточнення у бухгалтера.
    /// </summary>
    public bool HasUnfavorable { get; set; }
    /// <summary>
    /// Надбавка за складність/напруженість. Раньше була HasComplexityBonus bool на Employee. Мама: 5-50%, decimal, по
    /// наказу, на конкретну ставку. Уехала с Employee на EmployeePosition (приказ на ставку, не на человека). Доступна для
    /// всіх класів в теорії.
    /// </summary>
    public decimal? ComplexityBonusPct { get; set; }
    /// <summary>
    /// Престижність педагогічної праці. Нове поле.  
    /// Тільки для Class 1 (учителя). 5-20% по приказу.
    /// </summary>
    public decimal? PrestigeBonusPct { get; set; }
    /// <summary>
    /// Для директорозалежних посад — частка від окладу директора (заступник 0.95, головбух 0.90).
    /// Оклад тоді = оклад_директора × DirectorPct. null — посада рахується від власного розряду.
    /// </summary>
    public decimal? DirectorPct { get; set; }
    public EmployeeWorkload? Workload { get; set; }
    public EmployeeAdmin? Admin { get; set; }
    public EmployeeGpd? Gpd { get; set; }
    public EmployeePkr? Pkr { get; set; }
    public EmployeeNonPedagogical? NonPedagogical { get; set; }
}
