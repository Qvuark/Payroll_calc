namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Повний вхід рушія для ОДНОГО працівника за місяць. Збирається в Application з БД
/// (Employee + ставки + блоки надбавок + табель + SystemParams), щоб сам рушій (Calculation)
/// не залежав від БД і легко перевірявся diff-тестом по еталону.
/// </summary>
public record CalcInput
{
    public required int EmployeeId { get; init; }
    public required string FullName { get; init; }
    /// <summary>
    /// ІПН працівника (для розрахункового листа). Порожній — не вказано.
    /// </summary>
    public string TaxId { get; init; } = "";
    public required int Year { get; init; }
    public required int Month { get; init; }
    /// <summary>
    /// Норма робочих днів місяця (WorkCalendar) — дільник пропорції /норма×відпрацьовано.
    /// </summary>
    public required int NormDays { get; init; }
    /// <summary>
    /// Відпрацьовано днів за місяць (табель) — чисельник пропорції.
    /// </summary>
    public required decimal WorkedDays { get; init; }
    /// <summary>
    /// Відсоток податкової соц.пільги — для шапки розрахункового листа. Null = пільги немає.
    /// </summary>
    public decimal? SocialBenefitPct { get; init; }
    /// <summary>
    /// Ставки/посади працівника (1 Employee → N посад), кожна рахується окремо.
    /// </summary>
    public required IReadOnlyList<PositionCalcInput> Positions { get; init; }
    /// <summary>
    /// Ручні разові суми за місяць (премія, аванс, лікарняні, відпускні тощо).
    /// </summary>
    public required ManualAdjustments Manual { get; init; }
    /// <summary>
    /// Системні параметри — знімок SystemParams на цей місяць.
    /// </summary>
    public required PayrollParams Params { get; init; }
}
