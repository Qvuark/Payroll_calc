namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Спільний контракт persona-полів для DTO-рядків імпорту (Staff, Teachers, ...).
/// Дозволяє EmployeeUpserter працювати з будь-яким потоком: він бачить тільки
/// поля Employee і не знає про специфіку Staff/Teachers blocks.
/// Реалізують усі *RowDto, які мапляться на сутність Employee.
/// </summary>
public interface IPersonaRow
{
    /// <summary>
    /// 1-based номер рядка у файлі — для error-reporting у звіт.
    /// </summary>
    int RowIndex { get; }
    string? TabNumber { get; }
    string? FullName { get; }
    /// <summary>
    /// ІПН — природний ключ Employee. Group by TaxId в Importer = multi-position.
    /// </summary>
    string? TaxId { get; }
    DateOnly? HireDate { get; }
    string? Education { get; }
    /// <summary>
    /// Звання як рядок ("Старший вчитель", "Методист", ...).
    /// Importer резолвить у TitleTypeId з урахуванням WorkerClass scope.
    /// </summary>
    string? TitleType { get; }
    bool IsHonored { get; }
    decimal? HonoredAmount { get; }
    int? PedExperienceYears { get; }
    int? GeneralExperienceYears { get; }
    decimal? SocialBenefitPct { get; }
}
