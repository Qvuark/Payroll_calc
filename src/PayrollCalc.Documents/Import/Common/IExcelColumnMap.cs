namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Опис схеми колонок Excel-файлу: де лежать заголовки, які колонки очікуються,
/// як вони називаються укр. мовою у шаблоні.
/// Один інтерфейс — багато реалізацій (TeachersColumnMap, StaffColumnMap, ...).
/// Споживачі: HeaderValidator (звіряє заголовки) і TemplateGenerator
/// (формує порожній .xlsx з тими ж заголовками).
/// </summary>
public interface IExcelColumnMap
{
    /// <summary>
    /// 0-based номер рядка з EN-заголовками. Те, що читає парсер.
    /// </summary>
    int HeaderRowIndex { get; }
    /// <summary>
    /// 0-based номер рядка з укр. підписами колонок. Те, що бачить мама у шаблоні.
    /// </summary>
    int DescriptionRowIndex { get; }
    /// <summary>
    /// 0-based номер першого рядка з даними. Усе нижче — записи для імпорту.
    /// </summary>
    int FirstDataRowIndex { get; }
    /// <summary>
    /// Очікувані EN-заголовки: номер колонки → текст заголовка.
    /// IReadOnly — споживач не може випадково змінити схему.
    /// </summary>
    IReadOnlyDictionary<int, string> ExpectedHeaders { get; }
    /// <summary>
    /// Укр. підписи колонок для шаблону: номер колонки → опис українською.
    /// </summary>
    IReadOnlyDictionary<int, string> Descriptions { get; }
    /// <summary>
    /// Рядки-пояснення під таблицею (як заповнювати). Дефолт порожньо — staff/teachers без легенди.
    /// Парсер їх не зачіпає: пишуться в колонку A без TaxId, тож skip-empty їх пропускає.
    /// </summary>
    IReadOnlyList<string> FooterNotes => [];
}
