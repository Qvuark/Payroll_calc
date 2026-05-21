namespace PayrollCalc.Core.Entities;

/// <summary>
/// Підрозділ школи (Адміністрація, Педагогічний персонал, Спеціалісти, Господарська служба).
/// Логічне групування посад для звітів та UI.
/// </summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}