using PayrollCalc.Documents.Import.Staff;
using PayrollCalc.Infrastructure.Data;
using PayrollCalc.Documents.Import.Common;
namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Оркестратор імпорту staff.xlsx. Пайплайн: Stream → Parser → group by TaxId →
/// EmployeeUpserter + PositionUpserter per group → 1 SaveChanges на весь файл → ImportReport.
/// Атомарність забезпечена транзакцією EF: будь-який збій → відкат усього файлу.
/// </summary>
public class StaffImporter
{
    private readonly StaffParser _parser;
    private readonly EmployeeUpserter _employeeUpserter;
    private readonly PositionUpserter _positionUpserter;
    private readonly AppDbContext _db;
    public StaffImporter(StaffParser parser, EmployeeUpserter employeeUpserter, PositionUpserter positionUpserter, AppDbContext db)
    {
        _parser = parser;
        _employeeUpserter = employeeUpserter;
        _positionUpserter = positionUpserter;
        _db = db;
    }
    /// <summary>
    /// Імпорт staff.xlsx з потоку. Повертає звіт із кількістю розпарсених/імпортованих
    /// рядків та зведеним списком помилок (парсера + resolve в БД).
    /// </summary>
    public async Task<ImportReport> ImportAsync(Stream xlsx, CancellationToken ct = default)
    {
        var (rows, parseErrors) = _parser.Parse(xlsx);
        // Resolve-помилки upserter'ів збираємо окремо від парсерських — щоб у звіт пішли обидва пули без дублювання.
        var importErrors = new List<ParserError>();
        var createdRows = 0;
        var updatedRows = 0;
        var skippedRows = 0;
        // Одна людина = N рядків Excel (по одному на посаду). Group → 1 Employee upsert + N Position upsert на групу.
        var groups = rows.GroupBy(r => r.TaxId);
        foreach (var group in groups)
        {
            // Persona-поля однакові в усіх рядках групи — беремо перший як джерело.
            var firstRow = group.First();
            // empCreated потрібен для orphan-guard нижче: щойно створеного Employee без жодної
            // успішної позиції — відкочуємо. Existing Employee (empCreated=false) не чіпаємо.
            var (emp, empCreated) = await _employeeUpserter.UpsertAsync(firstRow, ct);
            var groupSucceeded = 0;
            foreach (var row in group)
            {
                var (ep, isCreated) = await _positionUpserter.UpsertAsync(emp, row, importErrors, ct);
                if (isCreated)
                {
                    createdRows++;
                    groupSucceeded++;
                }
                else if (ep is not null)
                {
                    updatedRows++;
                    groupSucceeded++;
                }
                else
                    skippedRows++;
            }
            // Orphan guard: щойно створений Employee без жодної успішної позиції → відкат з трекера.
            if (empCreated && groupSucceeded == 0)
                _db.Employees.Remove(emp);
        }
        // 1 коміт на весь файл = атомарність. Збій всередині циклу → відкат усіх змін.
        await _db.SaveChangesAsync(ct);
        return new ImportReport(createdRows, updatedRows, skippedRows, parseErrors.Concat(importErrors).ToList());
    }

}