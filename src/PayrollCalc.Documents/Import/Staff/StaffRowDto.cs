using PayrollCalc.Documents.Import.Common;
namespace PayrollCalc.Documents.Import.Staff;

/// <summary>
/// Один рядок staff.xlsx — типізована копія того, що в Excel.
/// Кілька рядків з одним TaxId = мульти-позиція (group by TaxId робить Importer).
/// Маппінг string→enum/Id (TitleType, Position) теж в Importer.
/// </summary>
public record StaffRowDto : IPersonaRow
{
    /// <summary>1-based номер рядка у файлі — для error-reporting бухгалтеру.</summary>
    public int RowIndex { get; init; }
    // ─── Persona (повторюється у мульти-рядках, береться з першого) ───
    public string? TabNumber { get; init; }
    public string? FullName { get; init; }
    public string? TaxId { get; init; }
    public DateOnly? HireDate { get; init; }
    public string? Education { get; init; }
    // ENUM як string (Class 2 — "Методист" / "Психолог-методист" / тощо), TitleTypeRepo резолвить у Id.
    public string? TitleType { get; init; }
    public bool IsHonored { get; init; }
    public decimal? HonoredAmount { get; init; }
    public int? PedExperienceYears { get; init; }
    public int? GeneralExperienceYears { get; init; }
    public decimal? SocialBenefitPct { get; init; }
    // ─── EmployeePosition (одна стрічка = одна позиція) ───
    public decimal? ComplexityPct { get; init; }
    public string? Position { get; init; }
    public DateOnly? PositionStartDate { get; init; }
    public int? TariffGrade { get; init; }
    public decimal? Stavki { get; init; }
    public bool IsPrimary { get; init; }
    public bool HasMilitary { get; init; }
    public bool HasUnfavorable { get; init; }
    // ─── ГПД (Class 2 — група продовженого дня) ───
    // Grade nullable (не призначено = null), Hours decimal default 0 (0 годин = валідно).
    public int? GpdGrade { get; init; }
    public decimal GpdHours { get; init; }
    // ─── ПКР (Class 2 — група короткотермінового перебування) ───
    public int? PkrGrade { get; init; }
    public decimal PkrHours { get; init; }
    // ─── Окремі надбавки сумою (decimal? — null = не призначено, 0 ≠ null) ───
    public decimal? MentorAmount { get; init; }
    public decimal? LibraryMgmtAmount { get; init; }
    public decimal? TextbooksAmount { get; init; }
    // ─── МОП-флаги (Class 4) ───
    public bool Disinfectants { get; init; }
    public bool NightShifts { get; init; }
}
