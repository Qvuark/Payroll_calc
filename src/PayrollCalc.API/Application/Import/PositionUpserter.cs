using PayrollCalc.Core.Entities;
using PayrollCalc.Documents.Import.Staff;
using PayrollCalc.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Insert-or-update EmployeePosition для конкретного Employee.
/// Resolve Position (за назвою) та TariffGrade (за номером) з довідників.
/// Якщо resolve не вдався — додає помилку у errors, повертає null (Importer пропустить строку).
/// SaveChangesAsync НЕ викликає — Importer комітить транзакцію на весь файл.
/// </summary>
public class PositionUpserter
{
    private readonly AppDbContext _db;
    public PositionUpserter(AppDbContext db) => _db = db;

    /// <summary>
    /// Створює нову EmployeePosition або оновлює існуючу за парою (EmployeeId, PositionId).
    /// Employee може бути ще не збереженим (Id=0) — EF підставить FK через nav property.
    /// Повертає (null, false) якщо resolve посади/розряду впав; (entity, true) при insert; (entity, false) при update.
    /// </summary>
    public async Task<(EmployeePosition? Entity, bool WasCreated)> UpsertAsync(
        Employee employee,
        StaffRowDto staffRow,
        List<ParserError> errors,
        CancellationToken ct = default)
    {
        var position = await _db.Positions.FirstOrDefaultAsync(p => p.Name == staffRow.Position, ct);
        if (position is null)
        {
            errors.Add(new ParserError(staffRow.RowIndex, "Position",
                $"Посада '{staffRow.Position}' не знайдена в довіднику"));
            return (null, false);
        }

        var tariffGrade = await _db.TariffGrades.FirstOrDefaultAsync(t => t.Grade == staffRow.TariffGrade, ct);
        if (tariffGrade is null)
        {
            errors.Add(new ParserError(staffRow.RowIndex, "TariffGrade",
                $"Розряд '{staffRow.TariffGrade}' не знайдено в довіднику"));
            return (null, false);
        }

        EmployeePosition? ep = null;
        if (employee.Id != 0)
        {
            ep = await _db.EmployeePositions.FirstOrDefaultAsync(
                x => x.EmployeeId == employee.Id && x.PositionId == position.Id, ct);
        }

        if (ep is null)
        {
            ep = new EmployeePosition
            {
                Employee = employee,
                PositionId = position.Id,
                TariffGradeId = tariffGrade.Id,
                RateCount = staffRow.Stavki!.Value,
                IsPrimary = staffRow.IsPrimary,
                HireDate = staffRow.HireDate!.Value,
                PositionStartDate = staffRow.PositionStartDate,
                EffectiveFrom = staffRow.PositionStartDate ?? staffRow.HireDate!.Value,
                HasMilitaryRecord = staffRow.HasMilitary,
                HasUnfavorable = staffRow.HasUnfavorable,
                ComplexityBonusPct = staffRow.ComplexityPct,
            };
            _db.EmployeePositions.Add(ep);
            return (ep, WasCreated: true);
        }

        ep.TariffGradeId = tariffGrade.Id;
        ep.RateCount = staffRow.Stavki!.Value;
        ep.IsPrimary = staffRow.IsPrimary;
        ep.HireDate = staffRow.HireDate!.Value;
        ep.PositionStartDate = staffRow.PositionStartDate;
        ep.EffectiveFrom = staffRow.PositionStartDate ?? staffRow.HireDate!.Value;
        ep.HasMilitaryRecord = staffRow.HasMilitary;
        ep.HasUnfavorable = staffRow.HasUnfavorable;
        ep.ComplexityBonusPct = staffRow.ComplexityPct;
        return (ep, WasCreated: false);
    }
}