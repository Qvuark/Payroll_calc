namespace PayrollCalc.Documents.Import.Teachers;

/// <summary>
/// Один рядок teachers.xlsx — типізована копія того, що в Excel.
/// Кілька рядків з одним TaxId = мульти-позиція (group by TaxId робить Importer).
/// Маппінг string→enum/Id (TitleType, Position, ClassMgmt, CabinetType) теж в Importer.
/// </summary>
public record TeachersRowDto
{
    /// <summary>1-based номер рядка у файлі — для error-reporting мамі.</summary>
    public int RowIndex { get; init; }
    // ─── Persona (повторюється у мульти-рядках, береться з першого) ───
    public string? TabNumber { get; init; }
    public string? FullName { get; init; }
    public string? TaxId { get; init; }
    public DateOnly? HireDate { get; init; }
    public string? Education { get; init; }
    // ENUM як string ("Старший вчитель"/"Вчитель-методист"), TitleTypeRepo резолвить у Id.
    public string? TitleType { get; init; }
    public bool IsHonored { get; init; }
    public decimal? HonoredAmount { get; init; }
    public int? PedExperienceYears { get; init; }
    public int? GeneralExperienceYears { get; init; }
    public decimal? SocialBenefitPct { get; init; }
    // ─── EmployeePosition (одна стрічка = одна позиція) ───
    public decimal? ComplexityPct { get; init; }
    public decimal? PrestigePct { get; init; }
    public string? Position { get; init; }
    public DateOnly? PositionStartDate { get; init; }
    public string? Subject { get; init; }
    public int? TariffGrade { get; init; }
    public decimal? Stavki { get; init; }
    public bool IsPrimary { get; init; }
    public bool HasMilitary { get; init; }
    public bool HasUnfavorable { get; init; }
    // ─── Workload (тільки Class 1, decimal бо години бувають 0.5/1.5) ───
    public decimal Hours1To4 { get; init; }
    public decimal IndividualHours1To4 { get; init; }
    public decimal Hours5To9 { get; init; }
    public decimal IndividualHours5To9 { get; init; }
    public decimal Hours10To11 { get; init; }
    public decimal IndividualHours10To11 { get; init; }
    public decimal NotebookHours1To4 { get; init; }
    public decimal NotebookHours5To9 { get; init; }
    public decimal NotebookHours10To11 { get; init; }
    public decimal InclusiveHours1To4 { get; init; }
    public decimal InclusiveHours5To9 { get; init; }
    public decimal InclusiveHours10To11 { get; init; }
    // ─── Педагогічні надбавки (Strategy B — тільки teachers.xlsx) ───
    // ENUM як string ("1-4" / "5-11" / null), Importer резолвить.
    public string? ClassMgmt { get; init; }
    // ENUM як string ("звичайний" / "музика-IT" / "майстерня" / null).
    public string? CabinetType { get; init; }
    public bool Gym { get; init; }
    public bool Shooting { get; init; }
    public bool Computers { get; init; }
    public bool Extracurricular { get; init; }
    public bool Website { get; init; }
}
