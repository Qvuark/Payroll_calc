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
        // ExcelReaderFactory сам визначає формат (.xls vs .xlsx) по байтах.
        // using гарантує що file handle закриється, навіть якщо нижче впаде exception.
        using var reader = ExcelReaderFactory.CreateReader(stream);
        // AsDataSet() читає всі листи у пам'ять. DataSet без using навмисно —
        // повертаємо DataTable з його колекції, dispose родителя зробив би таблицю невалідною.
        var dataset = reader.AsDataSet();
        return dataset.Tables[0];
    }
}
