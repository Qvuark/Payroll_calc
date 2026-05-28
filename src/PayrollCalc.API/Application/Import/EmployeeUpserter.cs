using PayrollCalc.Core.Entities;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Insert-or-update Employee по природному ключу TaxId.
/// НЕ викликає SaveChangesAsync — Importer комітить весь файл однією транзакцією.
/// Не торкає Positions (це робота PositionUpserter).
/// </summary>
public class EmployeeUpserter
{
    private readonly AppDbContext _db;
    public EmployeeUpserter(AppDbContext db) => _db = db;

    /// <summary>
    /// Знаходить Employee за TaxId; оновлює persona-поля з DTO або створює новий запис.
    /// Повертає сутність + прапор WasCreated (true=insert, false=update) — Importer лічить статистику.
    /// </summary>
    public async Task<(Employee Entity, bool WasCreated)> UpsertAsync(
        IPersonaRow row,
        CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(p => p.TaxId == row.TaxId, ct);
        if (employee is null)
        {
            employee = new Employee
            {
                TabNumber = row.TabNumber!,
                FullName = row.FullName!,
                TaxId = row.TaxId!,
                HireDate = row.HireDate!.Value,
                Education = row.Education,
                PedExperienceYears = row.PedExperienceYears ?? 0,
                GeneralExperienceYears = row.GeneralExperienceYears ?? 0,
                SocialBenefitPct = row.SocialBenefitPct,
                IsHonored = row.IsHonored,
                HonoredAmount = row.HonoredAmount,
                Status = EmployeeStatus.Active,
            };
            _db.Employees.Add(employee);
            return (employee, WasCreated: true);
        }

        employee.TabNumber = row.TabNumber!;
        employee.FullName = row.FullName!;
        employee.HireDate = row.HireDate!.Value;
        employee.Education = row.Education;
        employee.PedExperienceYears = row.PedExperienceYears ?? 0;
        employee.GeneralExperienceYears = row.GeneralExperienceYears ?? 0;
        employee.SocialBenefitPct = row.SocialBenefitPct;
        employee.IsHonored = row.IsHonored;
        employee.HonoredAmount = row.HonoredAmount;
        return (employee, WasCreated: false);
    }
}