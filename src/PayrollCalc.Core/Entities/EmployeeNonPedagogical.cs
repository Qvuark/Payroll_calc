namespace PayrollCalc.Core.Entities;

/// <summary>
/// Непедагогічні надбавки ставки: наставництво, завідування бібліотекою,
/// відповідальність за підручники, нічні зміни, дезінфектанти.
/// </summary>
public class EmployeeNonPedagogical
{
    public int EmployeePositionId { get; set; }
    public EmployeePosition? EmployeePosition { get; set; }
    /// <summary>
    /// Чи є наставництво (молодим вчителям).
    /// </summary>
    public bool HasMentor { get; set; } = false;
    /// <summary>
    /// Сума надбавки за наставництво. Задається бухгалтером по наказу.
    /// </summary>
    public decimal MentorAmount { get; set; } = decimal.Zero;
    /// <summary>
    /// Чи завідує бібліотекою.
    /// </summary>
    public bool HasLibraryMgmt { get; set; } = false;
    /// <summary>
    /// Сума надбавки за завідування бібліотекою. Задається бухгалтером по наказу.
    /// </summary>
    public decimal LibraryMgmtAmount { get; set; } = decimal.Zero;
    /// <summary>
    /// Чи відповідає за підручники.
    /// </summary>
    public bool HasTextbooks { get; set; } = false;
    /// <summary>
    /// Сума надбавки за підручники. Задається бухгалтером по наказу.
    /// </summary>
    public decimal TextbooksAmount { get; set; } = decimal.Zero;
    /// <summary>
    /// Чи працює з дезінфектантами (надбавка для МОП).
    /// </summary>
    public bool HasDisinfectants { get; set; } = false;
    /// <summary>
    /// Чи виходить у нічні зміни (для сторожів, МОП).
    /// </summary>
    public bool HasNightShifts { get; set; } = false;
}
