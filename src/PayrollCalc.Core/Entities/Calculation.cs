using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

public class Calculation
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal JSalary { get; set; } = decimal.Zero;
    public decimal NSalary { get; set; } = decimal.Zero;
    public decimal AllowancesTotal { get; set; } = decimal.Zero;
    public decimal GpdTotal { get; set; } = decimal.Zero;
    public decimal PkrTotal { get; set; } = decimal.Zero;
    public decimal SickEmployer { get; set; } = decimal.Zero;
    public decimal SickFss { get; set; } = decimal.Zero;
    public decimal VacationAmount { get; set; } = decimal.Zero;
    public decimal TrainingAmount { get; set; } = decimal.Zero;
    public decimal ManualTotal { get; set; } = decimal.Zero;
    public decimal GrossSalary { get; set; } = decimal.Zero;
    public decimal Pdfo { get; set; } = decimal.Zero;
    public decimal Vz { get; set; } = decimal.Zero;
    public decimal UnionFee { get; set; } = decimal.Zero;
    public decimal NetSalary { get; set; } = decimal.Zero;
    public decimal Esv { get; set; } = decimal.Zero;
    public CalculationStatus Status { get; set; } = CalculationStatus.Draft;
    public string ParamsSnapshot { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; } = DateTime.MinValue;
    public List<CalculationPeriod> Periods { get; set; } = [];
}
