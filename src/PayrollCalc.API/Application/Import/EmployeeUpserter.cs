using PayrollCalc.Core.Entities;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Створює або оновлює працівника (persona-поля: ПІБ, ІПН, стаж...) за ІПН.
/// Ставки не чіпає — це робота PositionUpserter. Не комітить — це робить Importer на весь файл.
/// </summary>
public class EmployeeUpserter(AppDbContext db)
{
    /// <summary>
    /// Шукає працівника за ІПН: знайшов — оновлює persona-поля, ні — створює нового.
    /// Повертає (працівник, WasCreated): true — створено, false — оновлено (Importer рахує статистику).
    /// </summary>
    public async Task<(Employee Entity, bool WasCreated)> UpsertAsync(
        IPersonaRow row,
        CancellationToken ct = default)
    {
        var employee = await db.Employees.FirstOrDefaultAsync(p => p.TaxId == row.TaxId, ct);
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
            db.Employees.Add(employee);
            return (employee, WasCreated: true);
        }

        // Status не чіпаємо: якщо звільнений знов з'явився у файлі — повторний прийом роблять свідомо через UI, а не тихо імпортом.
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