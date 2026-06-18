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
    /// Легасі-поле без споживача (немає ні в запиті, ні в розрахунку). Дроп колонки — разом
    /// з наступною міграцією-чисткою.
    /// </summary>
    public decimal OtherManual { get; set; } = decimal.Zero;
    /// <summary>
    /// Позакласна робота з фізвиховання (відомість AC). Ручна сума.
    /// </summary>
    public decimal PhysEducation { get; set; } = decimal.Zero;
    /// <summary>
    /// Компенсація за невикористану відпустку (відомість AQ).
    /// </summary>
    public decimal VacationCompensation { get; set; } = decimal.Zero;
    /// <summary>
    /// Оплата простою (відомість AR).
    /// </summary>
    public decimal Downtime { get; set; } = decimal.Zero;
    /// <summary>
    /// Оплата за час курсів підвищення кваліфікації (відомість AT).
    /// </summary>
    public decimal Courses { get; set; } = decimal.Zero;
    /// <summary>
    /// Індексація зарплати (відомість AV). Зараховується в базу доплати до МЗП.
    /// </summary>
    public decimal Indexation { get; set; } = decimal.Zero;
    /// <summary>
    /// Премія за місяць (відомість BB). Разова сума, рушій не рахує — вписує бухгалтер.
    /// </summary>
    public decimal Bonus { get; set; } = decimal.Zero;
    /// <summary>
    /// Лікарняні за рахунок роботодавця, перші 5 днів (відомість AL).
    /// </summary>
    public decimal SickEmployer { get; set; } = decimal.Zero;
    /// <summary>
    /// Лікарняні за рахунок ФСС (відомість AM). Зменшує базу профспілкового внеску.
    /// </summary>
    public decimal SickFss { get; set; } = decimal.Zero;
    /// <summary>
    /// Відпускні (відомість AZ).
    /// </summary>
    public decimal Vacation { get; set; } = decimal.Zero;
}