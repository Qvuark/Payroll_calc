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
/// Insert-or-update EmployeePosition разом з опціональними блоками (Workload / Admin) для Teachers потоку.
/// Резолвить Position (за назвою), TariffGrade (за номером), TitleType (per WorkerClass scope) та
/// NotebookRate (keyword-based по Subject). Мапить ClassMgmt / CabinetType string → enum.
/// Якщо resolve не вдався, mapping невалідний або ValidateBlocks повернув помилки — додає у errors,
/// повертає null (Importer пропустить рядок). SaveChangesAsync НЕ викликає — Importer комітить весь файл.
/// </summary>
public class TeachersPositionUpserter
{
    private readonly AppDbContext _db;
    public TeachersPositionUpserter(AppDbContext db) => _db = db;

    /// <summary>
    /// Створює нову EmployeePosition або оновлює існуючу за парою (EmployeeId, PositionId).
    /// Employee може бути ще не збереженим (Id=0) — EF підставить FK через nav property.
    /// Повертає (null, false) якщо resolve впав; (entity, true) при insert; (entity, false) при update.
    /// </summary>
    public async Task<(EmployeePosition? Entity, bool WasCreated)> UpsertAsync(
        Employee employee,
        TeachersRowDto row,
        List<ParserError> errors,
        CancellationToken ct = default)
    {
        var position = await _db.Positions.FirstOrDefaultAsync(p => p.Name == row.Position, ct);
        if (position is null)
        {
            errors.Add(new ParserError(row.RowIndex, "Position",
                $"Посада '{row.Position}' не знайдена в довіднику"));
            return (null, false);
        }
        var tariffGrade = await _db.TariffGrades.FirstOrDefaultAsync(t => t.Grade == row.TariffGrade, ct);
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
        // Престижність — лише педагогам (Class 1). Для решти класів % безглуздий: відсікаємо ввід.
        if (row.PrestigePct.HasValue && position.WorkerClass != WorkerClass.Pedagogical)
        {
            errors.Add(new ParserError(row.RowIndex, "PrestigePct",
                $"Надбавку за престижність дозволено лише педагогам (Class 1), а не класу '{position.WorkerClass}'"));
            return (null, false);
        }
        // Update path: підтягуємо існуючу EP разом з блоками через Include() —
        // інакше при upsert нижче EF не побачить старий блок і вставить дубль.
        EmployeePosition? ep = null;
        if (employee.Id != 0)
        {
            ep = await _db.EmployeePositions
                .Include(x => x.Workload)
                .Include(x => x.Admin)
                .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && x.PositionId == position.Id, ct);
        }

        // Прапори "чи присутній блок у рядку xlsx".
        // Workload → будь-яка з 12 hours-колонок > 0 (бо без годин блок безглуздий).
        // Admin → ClassMgmt/CabinetType заповнені АБО будь-який bool-флаг (Gym/Shooting/Computers/Extracurricular/Website).
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

        // Бізнес-валідація: чи дозволено цей набір блоків для WorkerClass посади.
        // Напр.: Specialist + Workload → "Спеціалісти не можуть мати навчальне навантаження".
        // hasGpd/hasPkr/hasNonPedagogical завжди false — це поля Staff потоку, у Teachers DTO їх нема.
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

        // ClassMgmt string → ClassGradeGroup enum mapping. Невідоме значення = ParserError + skip.
        ClassGradeGroup? classGradeGroup = null;
        if (!string.IsNullOrWhiteSpace(row.ClassMgmt))
        {
            classGradeGroup = row.ClassMgmt.Trim() switch
            {
                "1-4" => ClassGradeGroup.Grades1To4,
                "5-11" => ClassGradeGroup.Grades5To11,
                _ => null
            };
            if (classGradeGroup is null)
            {
                errors.Add(new ParserError(row.RowIndex, "ClassMgmt",
                    $"Невідома група класів '{row.ClassMgmt}' (очікувано '1-4' або '5-11')"));
                return (null, false);
            }
        }

        // CabinetType string → CabinetType enum mapping. Невідоме значення = ParserError + skip.
        CabinetType? cabinetType = null;
        if (!string.IsNullOrWhiteSpace(row.CabinetType))
        {
            cabinetType = row.CabinetType.Trim() switch
            {
                "звичайний" => CabinetType.Standard,
                "музика-IT" => CabinetType.MusicOrIT,
                "майстерня" => CabinetType.Workshop,
                _ => null
            };
            if (cabinetType is null)
            {
                errors.Add(new ParserError(row.RowIndex, "CabinetType",
                    $"Невідомий тип кабінету '{row.CabinetType}' (очікувано 'звичайний' / 'музика-IT' / 'майстерня')"));
                return (null, false);
            }
        }

        // NotebookRate резолвиться по keyword-match — NotebookRate.SubjectKeyword є підрядок Subject.
        // Тільки якщо Workload присутній (без годин блок не пишеться). Не знайдено = null без помилки
        // (деякі предмети як фізкультура не мають ставки за зошити — це не помилка).
        int? notebookRateId = null;
        if (hasWorkload && !string.IsNullOrWhiteSpace(row.Subject))
        {
            var subjectLower = row.Subject.ToLower();
            // Довідник малий (~6 рядків) — тягнемо весь і матчимо в пам'яті по межі слова.
            // Boundary-match (\b перед keyword) прибирає false-positive: "рукавички" більше не чіпляє "укр".
            var rates = await _db.NotebookRates.ToListAsync(ct);
            var notebookRate = rates.FirstOrDefault(r =>
                Regex.IsMatch(subjectLower, $@"\b{Regex.Escape(r.SubjectKeyword.ToLower())}"));
            if (notebookRate is not null)
                notebookRateId = notebookRate.Id;
        }

        // Insert або update базової EmployeePosition. PrestigeBonusPct — Teachers-only (Class 1).
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
            _db.EmployeePositions.Add(ep);
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

        // Звання прив'язане до ставки (scope = WorkerClass посади), бо одна людина може мати різні звання
        // на різних посадах. Пуста колонка ≠ "очистити" — резолвимо лише коли задано, інакше затерли б наявне.
        if (!string.IsNullOrWhiteSpace(row.TitleType))
        {
            ep.TitleTypeId = await TitleTypeResolver.ResolveTitleTypeIdAsync(
                _db, row.TitleType, position.WorkerClass, row.RowIndex, errors, ct);
        }

        // Workload upsert. ??= створює тільки коли блок відсутній (insert path або update без попереднього блоку).
        // Видалення НЕ робимо: пусте поле в xlsx ≠ "очистити блок" (bulk-імпорт = доповнення).
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

        // Admin upsert. ClassGradeGroup/CabinetType nullable — null коли тільки bool-флаги (наприклад Gym=true без класного керівництва).
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
