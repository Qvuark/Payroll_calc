using System.Data;
using System.Text;
using ExcelDataReader;

namespace PayrollCalc.Documents.Import.Common;

/// <summary>
/// Базовий рідер Excel-файлів. Відкриває потік, віддає перший лист як DataTable.
/// Не валідує вміст — це робота HeaderValidator + конкретних парсерів.
/// </summary>
public static class ExcelReaderBase
{
    static ExcelReaderBase()
    {
        // ExcelDataReader потребує codepage 1252 для .xls. .NET Core/9 його не має out-of-the-box.
        // Реєструємо тут, а не в Program.cs — так і тести (без Program.Main) працюють.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Читає перший лист .xlsx або .xls файлу з потоку.
    /// </summary>
    /// <param name="stream">Потік байтів файлу (наприклад, IFormFile.OpenReadStream()).</param>
    /// <returns>Перший лист як DataTable. Якщо лист порожній — DataTable з 0 рядків.</returns>
    public static DataTable ReadFirstSheet(Stream stream)
    {
        // Кривий/не-Excel файл (перейменований .txt, битий байт) → ExcelReaderFactory кидає
        // власний виняток. Без обгортки він би злетів у 500; перетворюємо на InvalidOperationException
        // з укр-текстом — GlobalExceptionHandler маппить такі на 400 з повідомленням користувачу.
        try
        {
            // ExcelReaderFactory сам визначає формат (.xls vs .xlsx) по байтах.
            // using гарантує що file handle закриється, навіть якщо нижче впаде exception.
            using var reader = ExcelReaderFactory.CreateReader(stream);
            // FilterSheet — читаємо в пам'ять ЛИШЕ перший лист: інші нам не потрібні,
            // а файл з десятком роздутих листів інакше з'їв би пам'ять процесу.
            // DataSet без using навмисно — повертаємо DataTable з його колекції,
            // dispose родителя зробив би таблицю невалідною.
            var dataset = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                FilterSheet = (_, sheetIndex) => sheetIndex == 0,
            });
            return dataset.Tables[0];
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                "Файл не є коректним Excel (.xlsx/.xls). Завантажте файл за наданим шаблоном.", ex);
        }
    }
}
