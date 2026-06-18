using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Validators;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Documents.Import.Teachers;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Створює або оновлює одну ставку вчителя + блоки Workload/Admin.
/// Резолвить назви/числа з файлу в Id довідників (Position, розряд, звання, ставка за зошити по предмету), мапить ClassMgmt/CabinetType в enum.
/// Помилка resolve чи валідації → запис у errors + null (Importer пропустить рядок). Не комітить — це робить Importer.
/// </summary>
public class TeachersPositionUpserter(AppDbContext db)
{
    /// <summary>
    /// Знаходить ставку за (EmployeeId, PositionId) і оновлює, або створює нову.
    /// Повертає (null, false) якщо resolve чи валідація впали (помилка вже в errors); (ставка, true) — створено; (ставка, false) — оновлено.
    /// </summary>
    public async Task<(EmployeePosition? Entity, bool WasCreated)> UpsertAsync(
        Employee employee,
        TeachersRowDto row,
        List<ParserError> errors,
        CancellationToken ct = default)
    {
        var position = await db.Positions.FirstOrDefaultAsync(p => p.Name == row.Position, ct);
        if (position is null)
        {
            errors.Add(new ParserError(row.RowIndex, "Position",
                $"Посада '{row.Position}' не знайдена в довіднику"));
            return (null, false);
        }
        var tariffGrade = await db.TariffGrades.FirstOrDefaultAsync(t => t.Grade == row.TariffGrade, ct);
        if (tariffGrade is null)
        {
            errors.Add(new ParserError(row.RowIndex, "TariffGrade",
                $"Розряд '{row.TariffGrade}' не знайдено в довіднику"));
            return (null, false);
        }
        if (!EmployeeValidator.ValidateGradeForClass(position.WorkerClass, tariffGrade.Grade))
        {
            errors.Add(new ParserError(row.RowIndex, "TariffGrade",
                $"Розряд {tariffGrade.Grade} не дозволено для класу '{position.WorkerClass}'"));
            return (null, false);
        }
        // Надбавку за престижність мають лише педагоги (Class 1) — для інших класів % не має сенсу.
        if (row.PrestigePct.HasValue && position.WorkerClass != WorkerClass.Pedagogical)
        {
            errors.Add(new ParserError(row.RowIndex, "PrestigePct",
                $"Надбавку за престижність дозволено лише педагогам (Class 1), а не класу '{position.WorkerClass}'"));
            return (null, false);
        }
        // Існуючу ставку тягнемо разом з блоками (Include), інакше EF не побачить старий блок і вставить дубль.
        EmployeePosition? ep = null;
        if (employee.Id != 0)
        {
            ep = await db.EmployeePositions
                .Include(x => x.Workload)
                .Include(x => x.Admin)
                .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && x.PositionId == position.Id, ct);
        }

        // Блок присутній якщо в рядку є його дані: Workload — будь-яка з 12 колонок годин > 0;
        // Admin — заповнені ClassMgmt/CabinetType або будь-який прапор (Gym/Shooting/Computers/Extracurricular/Website).
        var hasWorkload =
            row.Hours1To4 > 0 || row.IndividualHours1To4 > 0 ||
            row.Hours5To9 > 0 || row.IndividualHours5To9 > 0 ||
            row.Hours10To11 > 0 || row.IndividualHours10To11 > 0 ||
            row.NotebookHours1To4 > 0 || row.NotebookHours5To9 > 0 || row.NotebookHours10To11 > 0 ||
            row.InclusiveHours1To4 > 0 || row.InclusiveHours5To9 > 0 || row.InclusiveHours10To11 > 0;
        var hasAdmin =
            !string.IsNullOrWhiteSpace(row.ClassMgmt) ||
            !string.IsNullOrWhiteSpace(row.CabinetType) ||
            row.Gym || row.Shooting || row.Computers || row.Extracurricular || row.Website;

        // Чи дозволені ці блоки класу посади (напр. Спеціаліст + Workload → помилка).
        // Gpd/Pkr/NonPedagogical тут завжди false — це поля потоку Staff, у Teachers їх нема.
        var validationErrors = EmployeeValidator.ValidateBlocks(
            position.WorkerClass,
            hasWorkload: hasWorkload,
            hasAdmin: hasAdmin,
            hasGpd: false,
            hasPkr: false,
            hasNonPedagogical: false);
        if (validationErrors is not null)
        {
            foreach (var error in validationErrors)
                errors.Add(new ParserError(row.RowIndex, "WorkerClass",
                    $"Посада '{row.Position}': {error}"));
            return (null, false);
        }

        // ClassMgmt "1-4"/"5-11" → enum ClassGradeGroup. Невідоме значення → ParserError + skip.
        ClassGradeGroup? classGradeGroup = null;
        if (!string.IsNullOrWhiteSpace(row.ClassMgmt))
        {
            switch (row.ClassMgmt.Trim())
            {
                case "1-4":
                    classGradeGroup = ClassGradeGroup.Grades1To4;
                    break;
                case "5-11":
                    classGradeGroup = ClassGradeGroup.Grades5To11;
                    break;
            }
            if (classGradeGroup is null)
            {
                errors.Add(new ParserError(row.RowIndex, "ClassMgmt",
                    $"Невідома група класів '{row.ClassMgmt}' (очікувано '1-4' або '5-11')"));
                return (null, false);
            }
        }

        // Тип кабінету (рядок) → enum CabinetType. Невідоме значення → ParserError + skip.
        CabinetType? cabinetType = null;
        if (!string.IsNullOrWhiteSpace(row.CabinetType))
        {
            switch (row.CabinetType.Trim())
            {
                case "звичайний":
                    cabinetType = CabinetType.Standard;
                    break;
                case "музика-IT":
                    cabinetType = CabinetType.MusicOrIT;
                    break;
                case "майстерня":
                    cabinetType = CabinetType.Workshop;
                    break;
            }
            if (cabinetType is null)
            {
                errors.Add(new ParserError(row.RowIndex, "CabinetType",
                    $"Невідомий тип кабінету '{row.CabinetType}' (очікувано 'звичайний' / 'музика-IT' / 'майстерня')"));
                return (null, false);
            }
        }

        // Ставку за зошити шукаємо по предмету (SubjectKeyword входить у Subject), лише коли є Workload.
        // Не знайдено → null без помилки: деякі предмети (напр. фізкультура) надбавки за зошити не мають.
        int? notebookRateId = null;
        if (hasWorkload && !string.IsNullOrWhiteSpace(row.Subject))
        {
            var subjectLower = row.Subject.ToLower();
            // Довідник малий → тягнемо весь і матчимо в пам'яті. \b перед keyword прибирає хибні збіги (напр. "рукавички" не чіпляє "укр").
            var rates = await db.NotebookRates.ToListAsync(ct);
            var notebookRate = rates.FirstOrDefault(r =>
                Regex.IsMatch(subjectLower, $@"\b{Regex.Escape(r.SubjectKeyword.ToLower())}"));
            if (notebookRate is not null)
                notebookRateId = notebookRate.Id;
        }

        // Створюємо або оновлюємо ставку. PrestigeBonusPct — лише в потоці Teachers (Class 1).
        bool wasCreated;
        if (ep is null)
        {
            ep = new EmployeePosition
            {
                Employee = employee,
                PositionId = position.Id,
                TariffGradeId = tariffGrade.Id,
                RateCount = row.Stavki!.Value,
                IsPrimary = row.IsPrimary,
                HireDate = row.HireDate!.Value,
                PositionStartDate = row.PositionStartDate,
                EffectiveFrom = row.PositionStartDate ?? row.HireDate!.Value,
                HasMilitaryRecord = row.HasMilitary,
                HasUnfavorable = row.HasUnfavorable,
                ComplexityBonusPct = row.ComplexityPct,
                PrestigeBonusPct = row.PrestigePct,
            };
            db.EmployeePositions.Add(ep);
            wasCreated = true;
        }
        else
        {
            ep.TariffGradeId = tariffGrade.Id;
            ep.RateCount = row.Stavki!.Value;
            ep.IsPrimary = row.IsPrimary;
            ep.HireDate = row.HireDate!.Value;
            ep.PositionStartDate = row.PositionStartDate;
            ep.EffectiveFrom = row.PositionStartDate ?? row.HireDate!.Value;
            ep.HasMilitaryRecord = row.HasMilitary;
            ep.HasUnfavorable = row.HasUnfavorable;
            ep.ComplexityBonusPct = row.ComplexityPct;
            ep.PrestigeBonusPct = row.PrestigePct;
            wasCreated = false;
        }

        // Звання резолвимо лише коли воно задане: пуста колонка означає "не чіпати наявне", а не "очистити".
        if (!string.IsNullOrWhiteSpace(row.TitleType))
        {
            ep.TitleTypeId = await TitleTypeResolver.ResolveTitleTypeIdAsync(
                db, row.TitleType, position.WorkerClass, row.RowIndex, errors, ct);
        }

        // ??= створює блок лише коли його ще нема, інакше перезаписує поля.
        // Порожнє поле не очищає блок — імпорт тільки доповнює, не видаляє.
        if (hasWorkload)
        {
            ep.Workload ??= new EmployeeWorkload();
            var w = ep.Workload;
            w.Hours1To4 = row.Hours1To4;
            w.IndividualHours1To4 = row.IndividualHours1To4;
            w.Hours5To9 = row.Hours5To9;
            w.IndividualHours5To9 = row.IndividualHours5To9;
            w.Hours10To11 = row.Hours10To11;
            w.IndividualHours10To11 = row.IndividualHours10To11;
            w.NotebookHours1To4 = row.NotebookHours1To4;
            w.NotebookHours5To9 = row.NotebookHours5To9;
            w.NotebookHours10To11 = row.NotebookHours10To11;
            w.InclusiveHours1To4 = row.InclusiveHours1To4;
            w.InclusiveHours5To9 = row.InclusiveHours5To9;
            w.InclusiveHours10To11 = row.InclusiveHours10To11;
            w.NotebookRateId = notebookRateId;
        }

        // Admin: ClassGradeGroup/CabinetType лишаються null коли є тільки прапори (напр. Gym=true без класного керівництва).
        if (hasAdmin)
        {
            ep.Admin ??= new EmployeeAdmin();
            var a = ep.Admin;
            a.HasClassMgmt = classGradeGroup is not null;
            a.ClassGradeGroup = classGradeGroup;
            a.HasCabinet = cabinetType is not null;
            a.CabinetType = cabinetType;
            a.HasGym = row.Gym;
            a.HasShootingRange = row.Shooting;
            a.HasComputers = row.Computers;
            a.HasExtracurricular = row.Extracurricular;
            a.HasWebsite = row.Website;
        }

        return (ep, wasCreated);
    }
}
