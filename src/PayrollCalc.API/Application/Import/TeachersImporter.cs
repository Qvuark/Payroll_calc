using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Teachers;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Оркестратор імпорту teachers.xlsx: Stream → Parser → групуємо рядки по ІПН → upsert людини + її ставок (з блоками) → 1 SaveChanges на файл → звіт.
/// Один коміт = атомарність: будь-який збій відкочує весь файл.
/// </summary>
public class TeachersImporter
{
    private readonly TeachersParser _parser;
    private readonly EmployeeUpserter _employeeUpserter;
    private readonly TeachersPositionUpserter _positionUpserter;
    private readonly AppDbContext _db;
    public TeachersImporter(
        TeachersParser parser,
        EmployeeUpserter employeeUpserter,
        TeachersPositionUpserter positionUpserter,
        AppDbContext db)
    {
        _parser = parser;
        _employeeUpserter = employeeUpserter;
        _positionUpserter = positionUpserter;
        _db = db;
    }

    /// <summary>
    /// Імпортує teachers.xlsx з потоку. Повертає звіт: скільки створено/оновлено/пропущено + усі помилки (парсера й resolve).
    /// </summary>
    public async Task<ImportReport> ImportAsync(Stream xlsx, CancellationToken ct = default)
    {
        var (rows, parseErrors) = _parser.Parse(xlsx);
        // Помилки upserter'ів збираємо в окремий список від парсерських — у звіт підуть обидва пули.
        var importErrors = new List<ParserError>();
        var createdRows = 0;
        var updatedRows = 0;
        var skippedRows = 0;
        // Один вчитель = N рядків (по одному на предмет/посаду). Групуємо по ІПН → 1 upsert людини + N upsert ставок.
        var groups = rows.GroupBy(r => r.TaxId);
        foreach (var group in groups)
        {
            // Persona (ПІБ, стаж...) однакова в усіх рядках групи → беремо з першого.
            var firstRow = group.First();
            // Різні дати прийому в групі = ймовірна помилка вводу. Беремо з першого рядка, кладемо попередження у звіт. Не блокуємо.
            if (group.Select(r => r.HireDate).Distinct().Count() > 1)
                importErrors.Add(new ParserError(firstRow.RowIndex, "HireDate",
                    $"Увага: для ІПН {firstRow.TaxId} різні дати прийому в рядках групи — взято з першого рядка.",
                    ErrorSeverity.Warning));
            // empCreated потрібен для orphan-guard нижче: новоствореного працівника без жодної успішної ставки відкотимо. Існуючого не чіпаємо.
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
            // Orphan-guard: створили людину, але жодна ставка не пройшла → прибираємо її, щоб не лишилась без ставок.
            if (empCreated && groupSucceeded == 0)
                _db.Employees.Remove(emp);
        }
        // 1 коміт на весь файл = атомарність: збій → відкат усіх змін.
        await _db.SaveChangesAsync(ct);
        return new ImportReport(createdRows, updatedRows, skippedRows, parseErrors.Concat(importErrors).ToList());
    }
}
