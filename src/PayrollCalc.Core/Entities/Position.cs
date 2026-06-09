using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Entities;

/// <summary>
/// Довідник посад школи (директор, вчитель, бухгалтер, прибиральник...).
/// Носить WorkerClass — фундаментальну приналежність до однієї з 4 категорій,
/// яка визначає які блоки надбавок дозволені на ставці цієї посади.
/// </summary>
public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    /// <summary>
    /// Категорія працівника: 1=вчитель, 2=адмін-педагогічний, 3=спеціаліст, 4=МОП.
    /// Визначає набір дозволених блоків надбавок (див. EmployeeValidator.ValidateBlocks).
    /// </summary>
    public WorkerClass WorkerClass { get; set; }
    /// <summary>
    /// Погодинна посада (сторож): оклад = тариф/176×відпрацьовані_години, а не за дні.
    /// Мінімалка теж погодинна (МЗП/176×години).
    /// </summary>
    public bool IsHourly { get; set; }
    /// <summary>
    /// Скорочення з Excel-файлів які мапляться на цю позицію (jsonb-колонка у PG).
    /// Приклад: для "Вчитель" — ["вч.математики", "вч.етики", "вч.метод."].
    /// </summary>
    public List<string> ExcelAliases { get; set; } = [];
    public Department? Department { get; set; }
}