using System.ComponentModel.DataAnnotations;

namespace PayrollCalc.Core.DTOs.Employees.Requests;

/// <summary>
/// Запит на редагування існуючої ставки. PositionId і HireDate не міняються —
/// при зміні посади звільнити стару ставку (DismissalDate) і створити нову.
/// </summary>
public class UpdateEmployeePositionRequest
{
    [Required]
    public int TariffGradeId { get; set; }
    /// <summary>
    /// Кількість ставок: 0.5 = півставки, 1.0 = ставка, 1.5 = ставка з половиною.
    /// </summary>
    [Required, Range(0.01, 3.0)]
    public decimal RateCount { get; set; }
    /// <summary>
    /// Заповнюється при звільненні з цієї ставки. Інші активні ставки працівника не зачіпає.
    /// </summary>
    public DateOnly? DismissalDate { get; set; }
    public bool IsPrimary { get; set; }
    public bool HasMilitaryRecord { get; set; }
    public bool HasUnfavorable { get; set; }
    /// <summary>
    /// Дата початку роботи на цій посаді (для versioning). Null → береться HireDate.
    /// </summary>
    public DateOnly? PositionStartDate { get; set; }
    /// <summary>
    /// Звання на цій ставці (per-position). Null = без звання. WorkerClass звання має збігатися з посадою.
    /// </summary>
    public int? TitleTypeId { get; set; }
    [Range(0.05, 0.5)] public decimal? ComplexityBonusPct { get; set; }
    [Range(0.05, 0.2)] public decimal? PrestigeBonusPct { get; set; }
}
