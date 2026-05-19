namespace PayrollCalc.Documents.Import.Tarification;

/// <summary>
/// Рядок тарифної сітки (Tarification row) – одна тарифікаційна картка.
/// </summary>
public record TarificationRowDto
{
    public int RowIndex { get; init; }
    public string? TabNumber { get; init; }
    public string? FullName { get; init; }
    public string? Position { get; init; }
    public string? Title { get; init; }
    public string? Education { get; init; }
    public string? Category { get; init; }
    public string? TeachingExperience { get; init; }
    public BlockBaseDto? Base { get; init; }
    public BlockWorkloadDto? Workload { get; init; }
    public BlockAdminDto? Admin { get; init; }
    public BlockAllowancesDto? Allowances { get; init; }
    public BlockGpdDto? Gpd { get; init; }
    public BlockPkrDto? Pkr { get; init; }
    public BlockNonPedagogicalDto? NonPedagogical { get; init; }
    public Dictionary<int, decimal?> Controls { get; init; } = new();
    public string? FullNameVerification { get; init; }
}
/// <summary>
/// Block "Base" — тарифний розряд + ставка на місяць (cols 8, 9).
/// </summary>
public record BlockBaseDto
{
    public int? TariffGrade { get; init; }
    public decimal? MonthlyRate { get; init; }
}

/// <summary>
/// Block "Workload" — навчальне навантаження по класах (cols 18-20, 22-24, 26-27, 67).
/// </summary>
public record BlockWorkloadDto
{
    public decimal? Hours1_4 { get; init; }
    public decimal? IndividualHours1_4 { get; init; }
    public decimal? InclusiveHours1_4 { get; init; }
    public decimal? Hours5_9 { get; init; }
    public decimal? IndividualHours5_9 { get; init; }
    public decimal? InclusiveHours5_9 { get; init; }
    public decimal? Hours10_11 { get; init; }
    public decimal? IndividualHours10_11 { get; init; }
    public decimal? InclusiveHoursTotal { get; init; }
}
/// <summary>
/// Block "Admin" — позитивні % надбавки + admin oklad (cols 51, 53, 54, 56, 58, 60, 62, 64, 86, 87).
/// </summary>
public record BlockAdminDto
{
    public decimal? ClassLeaderPct { get; init; }
    public decimal? OfficeMaintenancePct { get; init; }
    public decimal? GymPct { get; init; }
    public decimal? ShootingRangePct { get; init; }
    public decimal? ComputerPct { get; init; }
    public decimal? ExtracurricularPct { get; init; }
    public decimal? WebsitePct { get; init; }
    public bool? HasAdminBonus1749 { get; init; }
    public string? OfficeName { get; init; }
    public decimal? AdminRate { get; init; }
}
/// <summary>
/// Block "Allowances" — % надбавки за вислугу, звання, зошити, інклюзив (cols 10, 11, 13, 15, 16).
/// </summary>
public record BlockAllowancesDto
{
    public decimal? TenurePct { get; init; }
    public bool? HasBonus1749 { get; init; }
    public decimal? TitlePct { get; init; }
    public decimal? NotebookPct { get; init; }
    public decimal? InclusivePct { get; init; }
}

/// <summary>
/// Block "NonPedagogical" — бібліотекар, підручники, педагог-наставник (cols 83-85).
/// </summary>
public record BlockNonPedagogicalDto
{
    public string? LibraryHead { get; init; }
    public decimal? TextbookAmount { get; init; }
    public decimal? MentorAmount { get; init; }
}
/// <summary>
/// Block "ГПД — група подовженого дня" (cols 72-74).
/// </summary>
public record BlockGpdDto
{
    public int? TariffGrade { get; init; }
    public decimal? Hours { get; init; }
    public bool? HasBonus1749 { get; init; }
}
/// <summary>
/// Block "ПКР — позакласна робота" (cols 77, 78, 80).
/// </summary>
public record BlockPkrDto
{
    public int? TariffGrade { get; init; }
    public decimal? Hours { get; init; }
    public bool? HasBonus1749 { get; init; }
}
