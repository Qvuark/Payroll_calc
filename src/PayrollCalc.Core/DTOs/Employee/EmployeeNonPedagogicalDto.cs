using PayrollCalc.Core.Entities;
namespace PayrollCalc.Core.DTOs.Employee;

public class EmployeeNonPedagogicalDto
{
    public int EmployeePositionId { get; set; }
    public bool HasDisinfectants { get; set; } = false;
    public bool HasNightShifts { get; set; } = false;
    public bool HasMentor { get; set; } = false;
    public decimal MentorAmount { get; set; } = decimal.Zero;
    public bool HasLibraryMgmt { get; set; } = false;
    public decimal LibraryMgmtAmount { get; set; } = decimal.Zero;
    public bool HasTextbooks { get; set; } = false;
    public decimal TextbooksAmount { get; set; } = decimal.Zero;
    public static EmployeeNonPedagogicalDto FromEntity(EmployeeNonPedagogical e)
    {
        return new EmployeeNonPedagogicalDto()
        {
            EmployeePositionId = e.EmployeePositionId,
            HasDisinfectants = e.HasDisinfectants,
            HasNightShifts = e.HasNightShifts,
            HasMentor = e.HasMentor,
            MentorAmount = e.MentorAmount,
            HasLibraryMgmt = e.HasLibraryMgmt,
            LibraryMgmtAmount = e.LibraryMgmtAmount,
            HasTextbooks = e.HasTextbooks,
            TextbooksAmount = e.TextbooksAmount
        };
    }
}