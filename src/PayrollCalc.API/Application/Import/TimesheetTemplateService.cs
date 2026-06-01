using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Export;
using PayrollCalc.Documents.Import.Timesheet;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Будує pre-filled timesheet.xlsx: тягне активних працівників + головну посаду з БД,
/// формує рядки ростера й віддає в TemplateGenerator. Documents-шар лишається без DbContext —
/// запит до БД живе тут, в Application.
/// </summary>
public class TimesheetTemplateService(AppDbContext db, TemplateGenerator generator)
{
    /// <summary>
    /// Генерує шаблон на (year, month): рядок на кожного активного, pre-fill №/ІПН/таб/ПІБ/посада.
    /// Числа (відпрацьовано/заміна/нічні) лишаються порожні — їх вписує мама.
    /// </summary>
    /// <param name="year">Рік періоду — для назви листа.</param>
    /// <param name="month">Місяць періоду — для назви листа.</param>
    /// <param name="ct">Cancellation з боку клієнта.</param>
    /// <returns>Байти xlsx для file download.</returns>
    public async Task<byte[]> BuildAsync(int year, int month, CancellationToken ct = default)
    {
        var employees = await db.Employees
            .Where(e => e.Status != EmployeeStatus.Dismissed)
            .Include(e => e.Positions)
                .ThenInclude(p => p.Position)
            .OrderBy(e => e.FullName)
            .ToListAsync(ct);
        var rows = new List<IReadOnlyDictionary<int, string>>();
        var rowNo = 1;
        foreach (var employee in employees)
        {
            // Головна ставка → її посада у шаблон. Нема IsPrimary → перша наявна → порожньо.
            var primary = employee.Positions.FirstOrDefault(p => p.IsPrimary) ?? employee.Positions.FirstOrDefault();
            rows.Add(new Dictionary<int, string>
            {
                { TimesheetColumnMap.ColRowNo, rowNo.ToString() },
                { TimesheetColumnMap.ColTaxId, employee.TaxId },
                { TimesheetColumnMap.ColTabNumber, employee.TabNumber },
                { TimesheetColumnMap.ColFullName, employee.FullName },
                { TimesheetColumnMap.ColPosition, primary?.Position?.Name ?? string.Empty },
            });
            rowNo++;
        }
        return generator.Generate(new TimesheetColumnMap(), rows, $"Табель {month:00}.{year}");
    }
}
