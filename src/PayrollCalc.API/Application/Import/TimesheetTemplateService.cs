using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Export;
using PayrollCalc.Documents.Import.Timesheet;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Будує pre-filled timesheet.xlsx: тягне активних працівників + головну посаду + блок навантаження з БД,
/// формує рядки ростера (ідентифікація + сіра довідка навантаження) й віддає в TemplateGenerator.
/// Запит до БД живе тут (Application), щоб Documents-шар лишався без DbContext.
/// </summary>
public class TimesheetTemplateService(AppDbContext db, TemplateGenerator generator)
{
    /// <summary>
    /// Генерує шаблон на (year, month): рядок на кожного активного. Pre-fill №/ІПН/таб/ПІБ/посада
    /// + сіра довідка навантаження (години по класах + ставки) з БД. Числа вводу лишаються порожні — їх вписує завуч.
    /// </summary>
    /// <param name="year">Рік періоду — для назви листа.</param>
    /// <param name="month">Місяць періоду — для назви листа.</param>
    /// <param name="ct">Cancellation з боку клієнта.</param>
    /// <returns>Байти xlsx для file download.</returns>
    public async Task<byte[]> BuildAsync(int year, int month, CancellationToken ct = default)
    {
        var employees = await db.Employees
            .Where(e => e.Status != EmployeeStatus.Dismissed)
            .Include(e => e.Positions).ThenInclude(p => p.Position)
            .Include(e => e.Positions).ThenInclude(p => p.Workload)
            // Сортування укр-алфавітом (collation), не по Unicode-кодах: інакше 'І' (U+0406) лізе поперед 'А'.
            .OrderBy(e => EF.Functions.Collate(e.FullName, "uk-UA-x-icu"))
            .ToListAsync(ct);
        var rows = new List<IReadOnlyDictionary<int, string>>();
        var rowNo = 1;
        foreach (var employee in employees)
        {
            // Беремо головну ставку (IsPrimary), її посаду — у шаблон. Нема головної → перша наявна → порожньо.
            var primary = employee.Positions.FirstOrDefault(p => p.IsPrimary) ?? employee.Positions.FirstOrDefault();
            var row = new Dictionary<int, string>
            {
                { TimesheetColumnMap.ColRowNo, rowNo.ToString() },
                { TimesheetColumnMap.ColTaxId, employee.TaxId },
                { TimesheetColumnMap.ColTabNumber, employee.TabNumber },
                { TimesheetColumnMap.ColFullName, employee.FullName },
                { TimesheetColumnMap.ColPosition, primary?.Position?.Name ?? string.Empty },
            };
            // Сіра довідка навантаження — з блоку Workload головної ставки (його мають лише вчителі).
            // 0 не пишемо: порожня клітинка = нема навантаження в групі, як у паперовому табелі.
            var wl = primary?.Workload;
            if (wl is not null)
            {
                AddIfNonZero(row, TimesheetColumnMap.ColTariff1To4, wl.Hours1To4);
                AddIfNonZero(row, TimesheetColumnMap.ColTariffInd1To4, wl.IndividualHours1To4);
                AddIfNonZero(row, TimesheetColumnMap.ColTariff5To9, wl.Hours5To9);
                AddIfNonZero(row, TimesheetColumnMap.ColTariffInd5To9, wl.IndividualHours5To9);
                AddIfNonZero(row, TimesheetColumnMap.ColTariff10To11, wl.Hours10To11);
                AddIfNonZero(row, TimesheetColumnMap.ColTariffInd10To11, wl.IndividualHours10To11);
            }
            // Сума ставок працівника по всіх активних посадах (1.5 = ставка + півставки).
            var rateCount = employee.Positions.Where(p => p.DismissalDate is null).Sum(p => p.RateCount);
            AddIfNonZero(row, TimesheetColumnMap.ColRateCount, rateCount);

            rows.Add(row);
            rowNo++;
        }
        return generator.Generate(new TimesheetColumnMap(), rows, $"Табель {month:00}.{year}");
    }

    // Пише значення у колонку лише якщо воно не нуль — порожня клітинка читається завучем як "нема".
    private static void AddIfNonZero(Dictionary<int, string> row, int col, decimal value)
    {
        if (value != 0)
            row[col] = value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
