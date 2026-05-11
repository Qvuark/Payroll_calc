namespace PayrollCalc.Core.Entities;

public class AvgSalaryInclusionRule
{
    public int Id { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IncludeSick { get; set; } = false;
    public bool IncludeVacation { get; set; } = false;
    public bool IncludeTraining { get; set; } = false;
    public bool IncludeCompensation { get; set; } = false;
    public DateOnly EffectiveDate { get; set; } = DateOnly.MinValue;
}