using PayrollCalc.Core.Entities.Enums;
using EmployeeEntity = PayrollCalc.Core.Entities.Employee;

namespace PayrollCalc.Core.DTOs.Employee;

/// <summary>
/// Скорочена картка працівника для списків (GET /api/employees).
/// Містить persona-поля + інформацію про головну (primary) ставку.
/// Для повного списку ставок з блоками використовувати EmployeeDetailDto.
/// </summary>
public class EmployeeSummaryDto
{
    public int Id { get; set; }
    public string TabNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public EmployeeStatus Status { get; set; }
    public string? PrimaryPositionName { get; set; }
    public string? PrimaryDepartmentName { get; set; }
    public WorkerClass? PrimaryWorkerClass { get; set; }
    /// <summary>
    /// Номер тарифного розряду головної ставки (1-25).
    /// </summary>
    public int? PrimaryTariffGrade { get; set; }
    public decimal? PrimaryRateCount { get; set; }
    /// <summary>
    /// Кількість активних ставок (без звільнених).
    /// </summary>
    public int ActivePositionsCount { get; set; } = 0;

    /// <summary>
    /// Маппінг entity → DTO. Primary-поля можуть бути null якщо у працівника
    /// немає primary-ставки (нова картка без ставок або всі ставки звільнені).
    /// </summary>
    /// <param name="e">Entity працівника з завантаженими Positions, Position, Department, TariffGrade.</param>
    /// <returns>DTO для списку.</returns>
    public static EmployeeSummaryDto FromEntity(EmployeeEntity e)
    {
        var primaryPosition = e.Positions.FirstOrDefault(p => p.IsPrimary && p.DismissalDate == null);
        return new EmployeeSummaryDto
        {
            Id = e.Id,
            TabNumber = e.TabNumber,
            FullName = e.FullName,
            Status = e.Status,
            PrimaryPositionName = primaryPosition?.Position?.Name,
            PrimaryDepartmentName = primaryPosition?.Position?.Department?.Name,
            PrimaryWorkerClass = primaryPosition?.Position?.WorkerClass,
            PrimaryTariffGrade = primaryPosition?.TariffGrade?.Grade,
            PrimaryRateCount = primaryPosition?.RateCount,
            ActivePositionsCount = e.Positions.Count(p => p.DismissalDate == null)
        };
    }
}
