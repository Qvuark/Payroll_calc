namespace PayrollCalc.Core.Interfaces;

/// <summary>
/// Маркер для довідників, які при імпорті резолвляться за назвою + синонімами з Excel.
/// Position і TitleType реалізують його, щоб AliasMatcher матчив обидва однаково.
/// </summary>
public interface IAliasable
{
    /// <summary>
    /// Канонічна назва запису довідника.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// Скорочення/синоніми з Excel, які теж мапляться на цей запис.
    /// </summary>
    List<string> ExcelAliases { get; }
}
