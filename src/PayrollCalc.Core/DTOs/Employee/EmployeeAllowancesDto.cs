using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeeAllowancesDto
{
    public int EmployeeId { get; set; }
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
    public static EmployeeAllowancesDto FromEntity(EmployeeAllowances e)
    {
        return new EmployeeAllowancesDto()
        {
            EmployeeId = e.EmployeeId,
            HasClassMgmt = e.HasClassMgmt,
            HasGym = e.HasGym,
            HasCabinet = e.HasCabinet,
            HasShootingRange = e.HasShootingRange,
            HasComputers = e.HasComputers,
            HasExtracurricular = e.HasExtracurricular,
            HasWebsite = e.HasWebsite,
            HasMentor = e.HasMentor,
            MentorAmount = e.MentorAmount,
            HasLibraryMgmt = e.HasLibraryMgmt,
            LibraryMgmtAmount = e.LibraryMgmtAmount,
            HasTextbooks = e.HasTextbooks,
            TextbooksAmount = e.TextbooksAmount,
            HasUnfavorable = e.HasUnfavorable,
            HasMilitaryAcct = e.HasMilitaryAcct,
            ClassGradeGroup = e.ClassGradeGroup,
            CabinetType = e.CabinetType
        };
    }
}