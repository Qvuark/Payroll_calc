using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

public class EmployeeAllowances
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool HasClassMgmt { get; set; } = false;
    public bool HasGym { get; set; } = false;
    public bool HasCabinet { get; set; } = false;
    public bool HasShootingRange { get; set; } = false;
    public bool HasComputers { get; set; } = false;
    public bool HasExtracurricular { get; set; } = false;
    public bool HasWebsite { get; set; } = false;
    public bool HasMentor { get; set; } = false;
    public decimal MentorAmount { get; set; } = decimal.Zero;
    public bool HasLibraryMgmt { get; set; } = false;
    public decimal LibraryMgmtAmount { get; set; } = decimal.Zero;
    public bool HasTextbooks { get; set; } = false;
    public decimal TextbooksAmount { get; set; } = decimal.Zero;
    public bool HasUnfavorable { get; set; } = false;
    public bool HasMilitaryAcct { get; set; } = false;
    public ClassGradeGroup? ClassGradeGroup { get; set; }
    public CabinetType? CabinetType { get; set; }
}