using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Employees;

/// <summary>
/// Блок навантаження ставки: години по класах (1-4, 5-9, 10-11),
/// індивідуальні, перевірка зошитів, інклюзивні. Дозволено тільки для Class 1 (вчителі).
/// </summary>
public class EmployeeWorkloadDto
{
    public int EmployeePositionId { get; set; }
    public decimal Hours1To4 { get; set; } = decimal.Zero;
    public decimal IndividualHours1To4 { get; set; } = decimal.Zero;
    public decimal Hours5To9 { get; set; } = decimal.Zero;
    public decimal IndividualHours5To9 { get; set; } = decimal.Zero;
    public decimal Hours10To11 { get; set; } = decimal.Zero;
    public decimal IndividualHours10To11 { get; set; } = decimal.Zero;
    public decimal NotebookHours1To4 { get; set; } = decimal.Zero;
    public decimal NotebookHours5To9 { get; set; } = decimal.Zero;
    public decimal NotebookHours10To11 { get; set; } = decimal.Zero;
    public decimal InclusiveHours1To4 { get; set; } = decimal.Zero;
    public decimal InclusiveHours5To9 { get; set; } = decimal.Zero;
    public decimal InclusiveHours10To11 { get; set; } = decimal.Zero;
    /// <summary>
    /// FK на NotebookRate — окремий розряд для оплати перевірки зошитів (не основний розряд ставки).
    /// </summary>
    public int? NotebookRateId { get; set; }
    /// <summary>
    /// Надтарифні години (понад ставку) — додаються до педнавантаження у формулі окладу.
    /// </summary>
    public decimal AdditionalHours { get; set; } = decimal.Zero;

    /// <summary>
    /// Маппінг entity → DTO. Plain field-by-field, без обчислень.
    /// </summary>
    /// <param name="e">Entity навантаження.</param>
    /// <returns>DTO для відповіді API.</returns>
    public static EmployeeWorkloadDto FromEntity(EmployeeWorkload e)
    {
        return new EmployeeWorkloadDto()
        {
            EmployeePositionId = e.EmployeePositionId,
            Hours1To4 = e.Hours1To4,
            IndividualHours1To4 = e.IndividualHours1To4,
            Hours5To9 = e.Hours5To9,
            IndividualHours5To9 = e.IndividualHours5To9,
            Hours10To11 = e.Hours10To11,
            IndividualHours10To11 = e.IndividualHours10To11,
            NotebookHours1To4 = e.NotebookHours1To4,
            NotebookHours5To9 = e.NotebookHours5To9,
            NotebookHours10To11 = e.NotebookHours10To11,
            InclusiveHours1To4 = e.InclusiveHours1To4,
            InclusiveHours5To9 = e.InclusiveHours5To9,
            InclusiveHours10To11 = e.InclusiveHours10To11,
            NotebookRateId = e.NotebookRateId,
            AdditionalHours = e.AdditionalHours
        };
    }
}