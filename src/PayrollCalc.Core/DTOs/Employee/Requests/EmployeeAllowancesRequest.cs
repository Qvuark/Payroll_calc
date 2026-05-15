using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee.Requests;

public class EmployeeAllowancesRequest
{
    public bool HasClassMgmt { get; set; }
    public ClassGradeGroup? ClassGradeGroup { get; set; }

    public bool HasCabinet { get; set; }
    public CabinetType? CabinetType { get; set; }

    public bool HasGym { get; set; }
    public bool HasShootingRange { get; set; }
    public bool HasComputers { get; set; }
    public bool HasExtracurricular { get; set; }
    public bool HasWebsite { get; set; }
    public bool HasMilitaryAcct { get; set; }
    public bool HasUnfavorable { get; set; }

    public bool HasMentor { get; set; }
    public decimal MentorAmount { get; set; }

    public bool HasLibraryMgmt { get; set; }
    public decimal LibraryMgmtAmount { get; set; }

    public bool HasTextbooks { get; set; }
    public decimal TextbooksAmount { get; set; }

    public static EmployeeAllowances FromRequest(EmployeeAllowancesRequest request)
    {
        return new EmployeeAllowances
        {
            HasClassMgmt = request.HasClassMgmt,
            ClassGradeGroup = request.ClassGradeGroup,
            HasCabinet = request.HasCabinet,
            CabinetType = request.CabinetType,
            HasGym = request.HasGym,
            HasShootingRange = request.HasShootingRange,
            HasComputers = request.HasComputers,
            HasExtracurricular = request.HasExtracurricular,
            HasWebsite = request.HasWebsite,
            HasMilitaryAcct = request.HasMilitaryAcct,
            HasUnfavorable = request.HasUnfavorable,
            HasMentor = request.HasMentor,
            MentorAmount = request.MentorAmount,
            HasLibraryMgmt = request.HasLibraryMgmt,
            LibraryMgmtAmount = request.LibraryMgmtAmount,
            HasTextbooks = request.HasTextbooks,
            TextbooksAmount = request.TextbooksAmount
        };
    }
}
