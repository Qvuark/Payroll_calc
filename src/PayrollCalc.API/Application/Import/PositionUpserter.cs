using PayrollCalc.Core.Entities;
using PayrollCalc.Documents.Import.Staff;
using PayrollCalc.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Core.Validators;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Insert-or-update EmployeePosition разом з опціональними блоками (Gpd / Pkr / NonPedagogical)
/// для конкретного Employee. Resolve Position (за назвою) та TariffGrade (за номером) з довідників.
/// Якщо resolve не вдався або ValidateBlocks повернув помилки — додає у errors, повертає null
/// (Importer пропустить строку). SaveChangesAsync НЕ викликає — Importer комітить транзакцію на весь файл.
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
        if (!EmployeeValidator.ValidateGradeForClass(position.WorkerClass, tariffGrade.Grade))
        {
            errors.Add(new ParserError(staffRow.RowIndex, "TariffGrade",
                $"Розряд {tariffGrade.Grade} не дозволено для класу '{position.WorkerClass}'"));
            return (null, false);
        }

        // Update path: знайти існуючу EP + одразу підтягти блоки через .Include() —
        // інакше при upsert нижче EF не побачить старого блока і вставить дублікат.
        // Insert path (Id=0 → нова Employee): ep = null, блоки створюються з нуля.
        EmployeePosition? ep = null;
        if (employee.Id != 0)
        {
            ep = await _db.EmployeePositions
                .Include(x => x.Gpd)
                .Include(x => x.Pkr)
                .Include(x => x.NonPedagogical)
                .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && x.PositionId == position.Id, ct);
        }

        // Прапори "чи присутній блок у рядку xlsx".
        // Gpd/Pkr → визначаємо по годинах (0 = блок не потрібен, бо без годин блок безглуздий).
        // NonPedagogical → HasValue для сум + bool flags для дезінфектантів/нічних (будь-яке поле = блок).
        var hasGpd = staffRow.GpdHours > 0;
        var hasPkr = staffRow.PkrHours > 0;
        var hasNonPedagogical = staffRow.MentorAmount.HasValue ||
                                staffRow.LibraryMgmtAmount.HasValue ||
                                staffRow.TextbooksAmount.HasValue ||
                                staffRow.Disinfectants ||
                                staffRow.NightShifts;

        // Бізнес-валідація: чи дозволено цей набір блоків для WorkerClass посади.
        // Напр.: Class 4 (MOP) + GpdHours > 0 → помилка "МОП не може мати ГПД".
        // hasWorkload/hasAdmin завжди false — це поля Teachers потоку, у Staff DTO їх нема.
        var validationErrors = EmployeeValidator.ValidateBlocks(
            position.WorkerClass,
            hasWorkload: false,
            hasAdmin: false,
            hasGpd: hasGpd,
            hasPkr: hasPkr,
            hasNonPedagogical: hasNonPedagogical
        );
        if (validationErrors is not null)
        {
            foreach (var error in validationErrors)
            {
                errors.Add(new ParserError(staffRow.RowIndex, "WorkerClass",
                    $"Посада '{staffRow.Position}': {error}"));
            }
            return (null, false);
        }
        // Gpd/Pkr мають ВЛАСНИЙ TariffGrade, окремий від EmployeePosition.TariffGradeId.
        // Бухгалтерські діапазони: ГПД = 10-14, ПКР = 10-12.
        // Тому resolve окремо. NonPedagogical — без свого розряду, доплати фіксованими сумами.
        TariffGrade? gpdGrade = null;
        if (hasGpd)
        {
            if (staffRow.GpdGrade is null)
            {
                errors.Add(new ParserError(
                    staffRow.RowIndex,
                    "GpdGrade",
                    $"Увага: Посада '{staffRow.Position}' належить до класу ГПД, тому має бути заповнений клас ГПД"
                ));
                return (null, false);
            }
            gpdGrade = await _db.TariffGrades.FirstOrDefaultAsync(t => t.Grade == staffRow.GpdGrade, ct);
            if (gpdGrade is null)
            {
                errors.Add(new ParserError(
                    staffRow.RowIndex,
                    "GpdGrade",
                    $"Увага: Розряд ГПД '{staffRow.GpdGrade}' не знайдено в довіднику"
                ));
                return (null, false);
            }
        }

        TariffGrade? pkrGrade = null;
        if (hasPkr)
        {
            if (staffRow.PkrGrade is null)
            {
                errors.Add(new ParserError(
                    staffRow.RowIndex,
                    "PkrGrade",
                    $"Увага: Посада '{staffRow.Position}' належить до класу ПКР, тому має бути заповнений клас ПКР"
                ));
                return (null, false);
            }
            pkrGrade = await _db.TariffGrades.FirstOrDefaultAsync(t => t.Grade == staffRow.PkrGrade, ct);
            if (pkrGrade is null)
            {
                errors.Add(new ParserError(
                    staffRow.RowIndex,
                    "PkrGrade",
                    $"Увага: Розряд ПКР '{staffRow.PkrGrade}' не знайдено в довіднику"
                ));
                return (null, false);
            }
        }
        // Insert або update базової EmployeePosition. Один-return-pattern щоб upsert блоків нижче
        // спрацював для обох гілок (insert + update). До рефактору insert path робив ранній return
        // і блоки Gpd/Pkr/NonPed для нових позицій ніколи не писались.
        bool wasCreated;
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
            wasCreated = true;
        }
        else
        {
            ep.TariffGradeId = tariffGrade.Id;
            ep.RateCount = staffRow.Stavki!.Value;
            ep.IsPrimary = staffRow.IsPrimary;
            ep.HireDate = staffRow.HireDate!.Value;
            ep.PositionStartDate = staffRow.PositionStartDate;
            ep.EffectiveFrom = staffRow.PositionStartDate ?? staffRow.HireDate!.Value;
            ep.HasMilitaryRecord = staffRow.HasMilitary;
            ep.HasUnfavorable = staffRow.HasUnfavorable;
            ep.ComplexityBonusPct = staffRow.ComplexityPct;
            wasCreated = false;
        }

        // Звання прив'язане до ставки (scope = WorkerClass посади), бо одна людина може мати різні звання
        // на різних посадах. Пуста колонка ≠ "очистити" — резолвимо лише коли задано, інакше затерли б наявне.
        if (!string.IsNullOrWhiteSpace(staffRow.TitleType))
        {
            ep.TitleTypeId = await TitleTypeResolver.ResolveTitleTypeIdAsync(
                _db, staffRow.TitleType, position.WorkerClass, staffRow.RowIndex, errors, ct);
        }

        // Upsert блоків. Працює для обох гілок: для insert path ep.Gpd завжди null (нова сутність),
        // для update path ep.Gpd підтягнутий через .Include() вище. ??= створює тільки коли відсутній.
        // Видалення блоків НЕ робимо: пусте поле в xlsx ≠ "очистити блок", імпорт — bulk-доповнення.
        if (hasGpd)
        {
            if (ep.Gpd is null)
                ep.Gpd = new EmployeeGpd
                {
                    TariffGradeId = gpdGrade!.Id,
                    GpdHours = staffRow.GpdHours
                };
            else
            {
                ep.Gpd.TariffGradeId = gpdGrade!.Id;
                ep.Gpd.GpdHours = staffRow.GpdHours;
            }
        }

        if (hasPkr)
        {
            if (ep.Pkr is null)
                ep.Pkr = new EmployeePkr
                {
                    TariffGradeId = pkrGrade!.Id,
                    PkrHours = staffRow.PkrHours
                };
            else
            {
                ep.Pkr.TariffGradeId = pkrGrade!.Id;
                ep.Pkr.PkrHours = staffRow.PkrHours;
            }
        }
        if (hasNonPedagogical)
        {
            ep.NonPedagogical ??= new EmployeeNonPedagogical();
            var np = ep.NonPedagogical;
            np.HasMentor = staffRow.MentorAmount.HasValue;
            np.MentorAmount = staffRow.MentorAmount ?? 0m;
            np.HasLibraryMgmt = staffRow.LibraryMgmtAmount.HasValue;
            np.LibraryMgmtAmount = staffRow.LibraryMgmtAmount ?? 0m;
            np.HasTextbooks = staffRow.TextbooksAmount.HasValue;
            np.TextbooksAmount = staffRow.TextbooksAmount ?? 0m;
            np.HasDisinfectants = staffRow.Disinfectants;
            np.HasNightShifts = staffRow.NightShifts;
        }

        return (ep, wasCreated);
    }
}