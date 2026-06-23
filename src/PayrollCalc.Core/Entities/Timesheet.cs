namespace PayrollCalc.Core.Entities;
/// <summary>
/// табель обліку робочого часу за місяць по працівнику
/// </summary>
public class Timesheet
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal WorkedDays { get; set; } = decimal.Zero;
    /// <summary>
    /// Відпрацьовані години за місяць (відомість D) — для погодинних посад (сторож). 0 у денних.
    /// </summary>
    public decimal WorkedHours { get; set; } = decimal.Zero;
    public decimal NightHours { get; set; } = decimal.Zero;
    public decimal HolidayAmount { get; set; } = decimal.Zero;
    public decimal ReplacementHours { get; set; } = decimal.Zero;
    public decimal Recalculation { get; set; } = decimal.Zero;
    public decimal Advance { get; set; } = decimal.Zero;
    public decimal EnforcementOrders { get; set; } = decimal.Zero;
    public decimal AnnualBonus { get; set; } = decimal.Zero;
    /// <summary>
    /// Позакласна робота з фізвиховання (відомість AC). Ручна сума.
    /// </summary>
    public decimal PhysEducation { get; set; } = decimal.Zero;
    /// <summary>
    /// Оплата простою (відомість AR).
    /// </summary>
    public decimal Downtime { get; set; } = decimal.Zero;
    /// <summary>
    /// Індексація зарплати (відомість AV). Зараховується в базу доплати до МЗП.
    /// </summary>
    public decimal Indexation { get; set; } = decimal.Zero;
    /// <summary>
    /// Премія за місяць (відомість BB). Разова сума, рушій не рахує — вписує бухгалтер.
    /// </summary>
    public decimal Bonus { get; set; } = decimal.Zero;
    /// <summary>
    /// Доплата за несприятливі умови праці — ручна надбавка понад почасову (відомість AY).
    /// Індивідуальні рішення бухгалтера: плоска 2600, її частина тощо.
    /// </summary>
    public decimal UnfavorableManual { get; set; } = decimal.Zero;
}