using PayrollCalc.Core.Entities;
using PayrollCalc.Documents.Import.Staff;
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
        StaffRowDto staffRow,
        CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(p => p.TaxId == staffRow.TaxId, ct);
        if (employee is null)
        {
            employee = new Employee
            {
                TabNumber = staffRow.TabNumber!,
                FullName = staffRow.FullName!,
                TaxId = staffRow.TaxId!,
                HireDate = staffRow.HireDate!.Value,
                Education = staffRow.Education,
                PedExperienceYears = staffRow.PedExperienceYears ?? 0,
                GeneralExperienceYears = staffRow.GeneralExperienceYears ?? 0,
                SocialBenefitPct = staffRow.SocialBenefitPct,
                IsHonored = staffRow.IsHonored,
                HonoredAmount = staffRow.HonoredAmount,
                Status = EmployeeStatus.Active,
            };
            _db.Employees.Add(employee);
            return (employee, WasCreated: true);
        }

        employee.TabNumber = staffRow.TabNumber!;
        employee.FullName = staffRow.FullName!;
        employee.HireDate = staffRow.HireDate!.Value;
        employee.Education = staffRow.Education;
        employee.PedExperienceYears = staffRow.PedExperienceYears ?? 0;
        employee.GeneralExperienceYears = staffRow.GeneralExperienceYears ?? 0;
        employee.SocialBenefitPct = staffRow.SocialBenefitPct;
        employee.IsHonored = staffRow.IsHonored;
        employee.HonoredAmount = staffRow.HonoredAmount;
        return (employee, WasCreated: false);
    }
}