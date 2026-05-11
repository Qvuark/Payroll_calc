namespace PayrollCalc.Core.Entities;

public class EmployeeWorkload
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
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
    public NotebookRate? NotebookRate { get; set; }
}