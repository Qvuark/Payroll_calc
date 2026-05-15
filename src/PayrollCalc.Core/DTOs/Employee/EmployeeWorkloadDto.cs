using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeeWorkloadDto
{
    public int EmployeeId { get; set; }
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
    public int NotebookRateId { get; set; }
    public static EmployeeWorkloadDto FromEntity(EmployeeWorkload e)
    {
        return new EmployeeWorkloadDto()
        {
            EmployeeId = e.EmployeeId,
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
            NotebookRateId = e.NotebookRateId
        };
    }
}