using PayrollCalc.Core.Entities;
using PayrollCalc.Documents.Import.Staff;
using PayrollCalc.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Documents.Import.Common;
using PayrollCalc.Core.Validators;

namespace PayrollCalc.API.Application.Import;

/// <summary>
/// Створює або оновлює одну ставку (EmployeePosition) + блоки Gpd/Pkr/NonPedagogical працівника.
/// Резолвить назви/числа з файлу в Id довідників і валідує клас. Не комітить — це робить Importer на весь файл.
/// </summary>
public class PositionUpserter
{
    private readonly AppDbContext _db;
    public PositionUpserter(AppDbContext db) => _db = db;

    /// <summary>
    /// Знаходить ставку за (EmployeeId, PositionId) і оновлює, або створює нову.
    /// Повертає (null, false) якщо resolve чи валідація впали (помилка вже в errors); (ставка, true) — створено; (ставка, false) — оновлено.
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
        // Існуючу ставку тягнемо разом з блоками (Include), інакше EF не побачить старий блок і вставить дубль.
        // Для нової людини (Id=0) шукати нічого — ep лишається null, блоки створяться з нуля.
        EmployeePosition? ep = null;
        if (employee.Id != 0)
        {
            ep = await _db.EmployeePositions
                .Include(x => x.Gpd)
                .Include(x => x.Pkr)
                .Include(x => x.NonPedagogical)
                .FirstOrDefaultAsync(x => x.EmployeeId == employee.Id && x.PositionId == position.Id, ct);
        }
        // Блок присутній якщо в рядку є його дані: Gpd/Pkr — години > 0, NonPedagogical — будь-яка сума чи прапор.
        var hasGpd = staffRow.GpdHours > 0;
        var hasPkr = staffRow.PkrHours > 0;
        var hasNonPedagogical = staffRow.MentorAmount.HasValue ||
                                staffRow.LibraryMgmtAmount.HasValue ||
                                staffRow.TextbooksAmount.HasValue ||
                                staffRow.Disinfectants ||
                                staffRow.NightShifts;

        // Чи дозволені ці блоки класу посади (напр. МОП + ГПД → помилка).
        // Workload/Admin тут завжди false — це поля потоку Teachers, у Staff їх нема.
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
        // ГПД і ПКР мають власний розряд, окремий від розряду ставки → резолвимо їх окремо.
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
        // Створюємо або оновлюємо ставку. Спільний шлях без раннього return — щоб блоки нижче писались і для нової, і для існуючої.
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

        // Звання резолвимо лише коли воно задане: пуста колонка означає "не чіпати наявне", а не "очистити".
        if (!string.IsNullOrWhiteSpace(staffRow.TitleType))
        {
            ep.TitleTypeId = await TitleTypeResolver.ResolveTitleTypeIdAsync(
                _db, staffRow.TitleType, position.WorkerClass, staffRow.RowIndex, errors, ct);
        }

        // ??= створює блок лише коли його ще нема, інакше перезаписує поля.
        // Порожнє поле не очищає блок — імпорт тільки доповнює, не видаляє.
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