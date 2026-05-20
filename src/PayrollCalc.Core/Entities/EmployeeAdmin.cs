using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

/// <summary>
/// Адмін-блок ставки. Несе керівні відсотки (директор/заступник) та надбавки
/// які прив'язані саме до цієї посади: класне керівництво, завідування кабінетом,
/// позакласна робота, ведення сайту.
/// </summary>
public class EmployeeAdmin
{
    public int EmployeePositionId { get; set; }
    public EmployeePosition? EmployeePosition { get; set; }
    /// <summary>
    /// Відсоток директорства від окладу (1.0 = 100%). Тільки для директора.
    /// </summary>
    public decimal DirectorPct { get; set; } = decimal.Zero;
    /// <summary>
    /// Кількість адмін-ставок (для заступника директора).
    /// </summary>
    public decimal AdminRateCount { get; set; } = decimal.Zero;
    /// <summary>
    /// Кількість педагогічних ставок (для заступника директора, який паралельно викладає).
    /// </summary>
    public decimal PedRateCount { get; set; } = decimal.Zero;
    /// <summary>
    /// Чи є класне керівництво на цій ставці.
    /// </summary>
    public bool HasClassMgmt { get; set; } = false;
    /// <summary>
    /// Група класів для розрахунку класного керівництва (1-4 чи 5-11).
    /// Null якщо HasClassMgmt = false.
    /// </summary>
    public ClassGradeGroup? ClassGradeGroup { get; set; }
    /// <summary>
    /// Чи завідує кабінетом.
    /// </summary>
    public bool HasCabinet { get; set; } = false;
    /// <summary>
    /// Тип кабінету (звичайний / музика-ІТ / майстерня). Впливає на відсоток надбавки.
    /// Null якщо HasCabinet = false.
    /// </summary>
    public CabinetType? CabinetType { get; set; }
    /// <summary>
    /// Чи завідує спортивним залом.
    /// </summary>
    public bool HasGym { get; set; } = false;
    /// <summary>
    /// Чи завідує тиром.
    /// </summary>
    public bool HasShootingRange { get; set; } = false;
    /// <summary>
    /// Чи завідує комп'ютерним класом.
    /// </summary>
    public bool HasComputers { get; set; } = false;
    /// <summary>
    /// Чи проводить позакласну роботу.
    /// </summary>
    public bool HasExtracurricular { get; set; } = false;
    /// <summary>
    /// Чи веде сайт школи.
    /// </summary>
    public bool HasWebsite { get; set; } = false;
}
