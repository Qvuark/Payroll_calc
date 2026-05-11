namespace PayrollCalc.Core.Entities;

public class EmployeeNonPedagogical
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool HasDisinfectants { get; set; } = false;
    public bool HasNightShifts { get; set; } = false;
}