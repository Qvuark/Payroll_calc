using System.Data;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Import.Teachers;

/// <summary>
/// Парсер teachers.xlsx. Stream → (List&lt;TeachersRowDto&gt;, List&lt;ParserError&gt;).
/// Не throws на bad data — збирає всі помилки у список, мама бачить повний звіт за один прохід.
/// Не знає про БД і бізнес-правила — це робота Importer (resolve string→Id, cross-record check).
/// </summary>
public class TeachersParser
{
    private readonly TeachersColumnMap _map = new();
    /// <summary>
    /// Public entry point — читає Stream, передає DataTable у ParseSheet.
    /// Тонка обгортка над ExcelReaderBase, основна логіка в ParseSheet (тестується ізольовано).
    /// </summary>
    public (List<TeachersRowDto> rows, List<ParserError> errors) Parse(Stream stream)
    {
        var sheet = ExcelReaderBase.ReadFirstSheet(stream);
        return ParseSheet(sheet);
    }

    /// <summary>
    /// Парсить готовий DataTable. Internal — щоб тести могли бити напряму DataTable
    /// без створення xlsx файлів (швидко, ізольовано). Виноситься окремо від Parse(Stream),
    /// бо логіка валідації + ітерації не залежить від джерела (Stream/xlsx/DataTable).
    /// </summary>
    internal (List<TeachersRowDto> rows, List<ParserError> errors) ParseSheet(DataTable sheet)
    {
        var rows = new List<TeachersRowDto>();
        var errors = new List<ParserError>();

        var headerErrors = HeaderValidator.Validate(
            sheet, _map.HeaderRowIndex, new Dictionary<int, string>(_map.ExpectedHeaders));
        if (headerErrors.Count > 0)
            return (rows, headerErrors);

        if (sheet.Rows.Count <= _map.FirstDataRowIndex)
        {
            errors.Add(new ParserError(
                _map.FirstDataRowIndex,
                null,
                "В файлі відсутні дані"));
            return (rows, errors);
        }

        for (int rowNumber = _map.FirstDataRowIndex; rowNumber < sheet.Rows.Count; rowNumber++)
        {
            var row = sheet.Rows[rowNumber];
            // +1 — у Excel нумерація рядків 1-based, у DataTable 0-based.
            // Передаємо у ParseRow саме "людський" номер, щоб мама бачила
            // у репорті помилок ту ж цифру, що в Excel.
            var dto = ParseRow(row, rowNumber + 1, errors);
            if (dto is not null)
                rows.Add(dto);
        }
        return (rows, errors);
    }
    /// <summary>
    /// Парсить одну строку Excel у TeachersRowDto. Повертає null якщо
    /// рядок порожній або відсутні mandatory поля. Помилки додає у errors,
    /// не throws — мама хоче бачити повний звіт за один прохід.
    /// </summary>
    private TeachersRowDto? ParseRow(DataRow row, int rowNumber, List<ParserError> errors)
    {
        // ─── Skip empty row ───
        // Якщо немає ані TabNumber, ані FullName — кінець даних або порожній
        // рядок-роздільник. Не помилка, тихо пропускаємо.
        var tabRaw = row[TeachersColumnMap.ColTabNumber]?.ToString();
        var nameRaw = row[TeachersColumnMap.ColFullName]?.ToString();
        if (string.IsNullOrWhiteSpace(tabRaw) && string.IsNullOrWhiteSpace(nameRaw))
            return null;
        // ─── Mandatory fields ───
        var tabNumber = GetMandatoryString(row, TeachersColumnMap.ColTabNumber, "TabNumber", rowNumber, errors);
        var fullName = GetMandatoryString(row, TeachersColumnMap.ColFullName, "FullName", rowNumber, errors);
        var taxId = GetMandatoryString(row, TeachersColumnMap.ColTaxId, "TaxId", rowNumber, errors);
        var hireDate = GetMandatoryDate(row, TeachersColumnMap.ColHireDate, "HireDate", rowNumber, errors);
        var position = GetMandatoryString(row, TeachersColumnMap.ColPosition, "Position", rowNumber, errors);
        var stavki = GetMandatoryDecimal(row, TeachersColumnMap.ColStavki, "Stavki", rowNumber, errors);
        var tariffGrade = GetMandatoryInt(row, TeachersColumnMap.ColTariffGrade, "TariffGrade", rowNumber, errors);
        // ─── TaxId format check (10 цифр) ───
        if (taxId is not null && (taxId.Length != 10 || !taxId.All(char.IsDigit)))
        {
            errors.Add(new ParserError(rowNumber, "TaxId", $"ІПН має складатися з 10 цифр, маємо '{taxId}'"));
            taxId = null;
        }
        // ─── Early return: без mandatory DTO бесполезен ───
        if (tabNumber is null || fullName is null || taxId is null || hireDate is null
            || position is null || stavki is null || tariffGrade is null)
            return null;
        // ─── Optional fields ───
        var education = GetOptionalString(row, TeachersColumnMap.ColEducation);
        var titleType = GetOptionalString(row, TeachersColumnMap.ColTitleType);
        var subject = GetOptionalString(row, TeachersColumnMap.ColSubject);
        var classMgmt = GetOptionalString(row, TeachersColumnMap.ColClassMgmt);
        var cabinetType = GetOptionalString(row, TeachersColumnMap.ColCabinetType);

        var positionStartDate = GetOptionalDate(row, TeachersColumnMap.ColPositionStartDate, "PositionStartDate", rowNumber, errors);

        var honoredAmount = GetOptionalDecimal(row, TeachersColumnMap.ColHonoredAmount, "HonoredAmount", rowNumber, errors);
        var socialBenefitPct = GetOptionalDecimal(row, TeachersColumnMap.ColSocialBenefitPct, "SocialBenefitPct", rowNumber, errors);
        var complexityPct = GetOptionalDecimal(row, TeachersColumnMap.ColComplexityPct, "ComplexityPct", rowNumber, errors);
        var prestigePct = GetOptionalDecimal(row, TeachersColumnMap.ColPrestigePct, "PrestigePct", rowNumber, errors);

        var pedExpYears = GetOptionalInt(row, TeachersColumnMap.ColPedExpYears, "PedExpYears", rowNumber, errors);
        var generalExpYears = GetOptionalInt(row, TeachersColumnMap.ColGeneralExpYears, "GeneralExpYears", rowNumber, errors);

        var isHonored = GetOptionalBool(row, TeachersColumnMap.ColIsHonored);
        var isPrimary = GetOptionalBool(row, TeachersColumnMap.ColIsPrimary);
        var hasMilitary = GetOptionalBool(row, TeachersColumnMap.ColHasMilitary);
        var hasUnfavorable = GetOptionalBool(row, TeachersColumnMap.ColHasUnfavorable);
        var gym = GetOptionalBool(row, TeachersColumnMap.ColGym);
        var shooting = GetOptionalBool(row, TeachersColumnMap.ColShooting);
        var computers = GetOptionalBool(row, TeachersColumnMap.ColComputers);
        var extracurricular = GetOptionalBool(row, TeachersColumnMap.ColExtracurricular);
        var website = GetOptionalBool(row, TeachersColumnMap.ColWebsite);

        // ─── Hours (decimal, default 0) ───
        var hours1To4 = GetOptionalHours(row, TeachersColumnMap.ColHours1To4, "Hours1To4", rowNumber, errors);
        var ind1To4 = GetOptionalHours(row, TeachersColumnMap.ColIndividualHours1To4, "IndividualHours1To4", rowNumber, errors);
        var hours5To9 = GetOptionalHours(row, TeachersColumnMap.ColHours5To9, "Hours5To9", rowNumber, errors);
        var ind5To9 = GetOptionalHours(row, TeachersColumnMap.ColIndividualHours5To9, "IndividualHours5To9", rowNumber, errors);
        var hours10To11 = GetOptionalHours(row, TeachersColumnMap.ColHours10To11, "Hours10To11", rowNumber, errors);
        var ind10To11 = GetOptionalHours(row, TeachersColumnMap.ColIndividualHours10To11, "IndividualHours10To11", rowNumber, errors);
        var notebook1To4 = GetOptionalHours(row, TeachersColumnMap.ColNotebookHours1To4, "NotebookHours1To4", rowNumber, errors);
        var notebook5To9 = GetOptionalHours(row, TeachersColumnMap.ColNotebookHours5To9, "NotebookHours5To9", rowNumber, errors);
        var notebook10To11 = GetOptionalHours(row, TeachersColumnMap.ColNotebookHours10To11, "NotebookHours10To11", rowNumber, errors);
        var inclusive1To4 = GetOptionalHours(row, TeachersColumnMap.ColInclusiveHours1To4, "InclusiveHours1To4", rowNumber, errors);
        var inclusive5To9 = GetOptionalHours(row, TeachersColumnMap.ColInclusiveHours5To9, "InclusiveHours5To9", rowNumber, errors);
        var inclusive10To11 = GetOptionalHours(row, TeachersColumnMap.ColInclusiveHours10To11, "InclusiveHours10To11", rowNumber, errors);

        return new TeachersRowDto
        {
            RowIndex = rowNumber,
            TabNumber = tabNumber,
            FullName = fullName,
            TaxId = taxId,
            HireDate = hireDate,
            Education = education,
            TitleType = titleType,
            IsHonored = isHonored,
            HonoredAmount = honoredAmount,
            PedExperienceYears = pedExpYears,
            GeneralExperienceYears = generalExpYears,
            SocialBenefitPct = socialBenefitPct,
            ComplexityPct = complexityPct,
            PrestigePct = prestigePct,
            Position = position,
            PositionStartDate = positionStartDate,
            Subject = subject,
            TariffGrade = tariffGrade,
            Stavki = stavki,
            IsPrimary = isPrimary,
            HasMilitary = hasMilitary,
            HasUnfavorable = hasUnfavorable,
            Hours1To4 = hours1To4,
            IndividualHours1To4 = ind1To4,
            Hours5To9 = hours5To9,
            IndividualHours5To9 = ind5To9,
            Hours10To11 = hours10To11,
            IndividualHours10To11 = ind10To11,
            NotebookHours1To4 = notebook1To4,
            NotebookHours5To9 = notebook5To9,
            NotebookHours10To11 = notebook10To11,
            InclusiveHours1To4 = inclusive1To4,
            InclusiveHours5To9 = inclusive5To9,
            InclusiveHours10To11 = inclusive10To11,
            ClassMgmt = classMgmt,
            CabinetType = cabinetType,
            Gym = gym,
            Shooting = shooting,
            Computers = computers,
            Extracurricular = extracurricular,
            Website = website,
        };
    }
    // ─── Mandatory helpers ───
    // Контракт: пусто АБО кривий формат → error + return null.
    // Caller перевіряє null у early-return, без mandatory DTO нема сенсу будувати.
    private static string? GetMandatoryString(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var val = row[col]?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(val))
        {
            errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' обов'язкове"));
            return null;
        }
        return val;
    }
    private static DateOnly? GetMandatoryDate(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        // row[col] віддаємо сирим — DateParser сам розбере DateTime/double/string,
        // ToString() з'їв би type info і ламав би Excel serial dates.
        if (DateParser.TryParse(row[col], out var date))
            return date;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' обов'язкове або має бути датою"));
        return null;
    }
    private static decimal? GetMandatoryDecimal(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        if (DecimalParser.TryParse(row[col], out var dec))
            return dec;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' обов'язкове або має бути числом"));
        return null;
    }
    private static int? GetMandatoryInt(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col]?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' обов'язкове"));
            return null;
        }
        if (int.TryParse(raw, out var i))
            return i;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути цілим числом"));
        return null;
    }
    // ─── Optional helpers ───
    // Різниця: пусто = null/default (без error), кривий формат = error + null/default.
    private static string? GetOptionalString(DataRow row, int col)
    {
        var val = row[col]?.ToString()?.Trim();
        return string.IsNullOrWhiteSpace(val) ? null : val;
    }
    private static DateOnly? GetOptionalDate(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col];
        if (raw is null || raw is DBNull || string.IsNullOrWhiteSpace(raw.ToString()))
            return null;
        if (DateParser.TryParse(raw, out var d))
            return d;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути датою"));
        return null;
    }
    private static decimal? GetOptionalDecimal(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col];
        if (raw is null || raw is DBNull || string.IsNullOrWhiteSpace(raw.ToString()))
            return null;
        if (DecimalParser.TryParse(raw, out var dec))
            return dec;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути числом"));
        return null;
    }
    private static int? GetOptionalInt(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col]?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        if (int.TryParse(raw, out var i))
            return i;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути цілим числом"));
        return null;
    }
    private static bool GetOptionalBool(DataRow row, int col)
    {
        // BoolParser.TryParse повертає false на null/пусто/невідоме значення.
        // Для bool поля default false — ок, мама не пометила = не активно.
        BoolParser.TryParse(row[col], out var b);
        return b;
    }
    /// <summary>
    /// Окремо від GetOptionalDecimal: повертає не <c>decimal?</c>, а <c>decimal</c>
    /// з дефолтом 0m. Hours-поля в DTO non-nullable, бо 0 годин = валідне значення
    /// (а не "не вказано"), і калькулятор передбачає decimal не decimal?.
    /// </summary>
    private static decimal GetOptionalHours(DataRow row, int col, string fieldName, int rowNumber, List<ParserError> errors)
    {
        var raw = row[col];
        if (raw is null || raw is DBNull || string.IsNullOrWhiteSpace(raw.ToString()))
            return 0m;
        if (DecimalParser.TryParse(raw, out var dec))
            return dec;
        errors.Add(new ParserError(rowNumber, fieldName, $"Поле '{fieldName}' має бути числом"));
        return 0m;
    }
}
