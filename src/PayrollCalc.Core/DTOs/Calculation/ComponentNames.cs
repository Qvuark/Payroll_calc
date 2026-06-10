namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Імена компонентів розрахунку, які шукаються за назвою поза рушієм
/// (збереження зведення у Calculation, колонки відомості). Винесені в константи,
/// щоб перейменування в рушії не дало мовчазні нулі при матчингу рядків.
/// </summary>
public static class ComponentNames
{
    public const string Pdfo = "ПДФО";
    public const string Vz = "Військовий збір";
    public const string UnionFee = "Профспілковий внесок";
    public const string Bonus = "Премія";
    public const string Vacation = "Відпускні";
    public const string SickEmployer = "Лікарняні (роботодавець)";
    public const string SickFss = "Лікарняні (ФСС)";
    public const string Recalculation = "Перерахунок";
    public const string Holiday = "Святкові";
    public const string AnnualBonus = "Щорічна винагорода";
    public const string Advance = "Аванс";
    public const string EnforcementOrders = "Виконавчі листи";
    public const string PhysEducation = "Позакласна робота з фізвиховання";
    public const string VacationCompensation = "Компенсація за невикористану відпустку";
    public const string Downtime = "Простій";
    public const string Courses = "Курси";
    public const string Indexation = "Індексація";
    public const string Disinfectants = "Дезінфікуючі засоби";
    public const string NightShift = "Доплата за нічні";
}
