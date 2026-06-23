using System.Data;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Import.Staff;

/// <summary>
/// Парсер staff.xlsx. Stream → (List&lt;StaffRowDto&gt;, List&lt;ParserError&gt;).
/// Не throws на bad data — збирає всі помилки у список, бухгалтер бачить повний звіт за один прохід.
/// Не знає про БД і бізнес-правила — це робота Importer (resolve string→Id, cross-record check).
/// </summary>
public class StaffParser
{
    private readonly StaffColumnMap _map = new();
    /// <summary>
    /// Public entry point — читає Stream, передає DataTable у ParseSheet.
    /// Тонка обгортка над ExcelReaderBase, основна логіка в ParseSheet (тестується ізольовано).
    /// </summary>
    public (List<StaffRowDto> rows, List<ParserError> errors) Parse(Stream stream)
    {
        var sheet = ExcelReaderBase.ReadFirstSheet(stream);
        return ParseSheet(sheet);
    }
    /// <summary>
    /// Парсить готовий DataTable. Internal — щоб тести могли бити напряму DataTable
    /// без створення xlsx файлів (швидко, ізольовано). Виноситься окремо від Parse(Stream),
    /// бо логіка валідації + ітерації не залежить від джерела (Stream/xlsx/DataTable).
    /// </summary>
    internal (List<StaffRowDto> rows, List<ParserError> errors) ParseSheet(DataTable sheet)
    {
        var dtos = new List<StaffRowDto>();
        var errors = new List<ParserError>();
        var headerErrors = HeaderValidator.Validate(
            sheet, _map.HeaderRowIndex, new Dictionary<int, string>(_map.ExpectedHeaders));
        if (headerErrors.Count > 0)
            return (dtos, headerErrors);
        if (sheet.Rows.Count <= _map.FirstDataRowIndex)
        {
            errors.Add(new ParserError(
                _map.FirstDataRowIndex,
                null,
                "В файлі відсутні дані"));
            return (dtos, errors);
        }
        for (int rowNumber = _map.FirstDataRowIndex; rowNumber < sheet.Rows.Count; rowNumber++)
        {
            var row = sheet.Rows[rowNumber];
            // +1 — у Excel нумерація рядків 1-based, у DataTable 0-based.
            // Передаємо у ParseRow саме "людський" номер, щоб бухгалтер бачив
            // у репорті помилок ту ж цифру, що в Excel.
            var dto = ParseRow(row, rowNumber + 1, errors);
            if (dto is not null)
                dtos.Add(dto);
        }
        return (dtos, errors);
    }
    /// <summary>
    /// Парсить одну строку Excel у StaffRowDto. Повертає null якщо
    /// рядок порожній або відсутні mandatory поля. Помилки додає у errors,
    /// не throws — бухгалтер хоче бачити повний звіт за один прохід.
    /// </summary>
    private StaffRowDto? ParseRow(DataRow row, int rowNumber, List<ParserError> errors)
    {
        // ─── Skip empty row ───
        // Якщо немає ані TabNumber, ані FullName — кінець даних або порожній
        // рядок-роздільник. Не помилка, тихо пропускаємо.
        var tabRaw = row[StaffColumnMap.ColTabNumber]?.ToString();
        var nameRaw = row[StaffColumnMap.ColFullName]?.ToString();
        if (string.IsNullOrWhiteSpace(tabRaw) && string.IsNullOrWhiteSpace(nameRaw))
            return null;
        // ─── Mandatory fields ───
        var tabNumber = ExcelFieldReader.GetMandatoryString(row, StaffColumnMap.ColTabNumber, "TabNumber", rowNumber, errors);
        var fullName = ExcelFieldReader.GetMandatoryString(row, StaffColumnMap.ColFullName, "FullName", rowNumber, errors);
        var taxId = ExcelFieldReader.GetMandatoryString(row, StaffColumnMap.ColTaxId, "TaxId", rowNumber, errors);
        var hireDate = ExcelFieldReader.GetMandatoryDate(row, StaffColumnMap.ColHireDate, "HireDate", rowNumber, errors);
        var position = ExcelFieldReader.GetMandatoryString(row, StaffColumnMap.ColPosition, "Position", rowNumber, errors);
        var stavki = ExcelFieldReader.GetMandatoryDecimal(row, StaffColumnMap.ColStavki, "Stavki", rowNumber, errors);
        var tariffGrade = ExcelFieldReader.GetMandatoryInt(row, StaffColumnMap.ColTariffGrade, "TariffGrade", rowNumber, errors);
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
        // ─── Optional strings ───
        var education = ExcelFieldReader.GetOptionalString(row, StaffColumnMap.ColEducation);
        var titleType = ExcelFieldReader.GetOptionalString(row, StaffColumnMap.ColTitleType);

        var positionStartDate = ExcelFieldReader.GetOptionalDate(row, StaffColumnMap.ColPositionStartDate, "PositionStartDate", rowNumber, errors);

        var honoredAmount = ExcelFieldReader.GetOptionalDecimal(row, StaffColumnMap.ColHonoredAmount, "HonoredAmount", rowNumber, errors);
        var socialBenefitPct = ExcelFieldReader.GetOptionalDecimal(row, StaffColumnMap.ColSocialBenefitPct, "SocialBenefitPct", rowNumber, errors);
        var complexityPct = ExcelFieldReader.GetOptionalDecimal(row, StaffColumnMap.ColComplexityPct, "ComplexityPct", rowNumber, errors);

        var pedExpYears = ExcelFieldReader.GetOptionalInt(row, StaffColumnMap.ColPedExpYears, "PedExpYears", rowNumber, errors);
        var generalExpYears = ExcelFieldReader.GetOptionalInt(row, StaffColumnMap.ColGeneralExpYears, "GeneralExpYears", rowNumber, errors);

        // ─── Booleans ───
        var isHonored = ExcelFieldReader.GetOptionalBool(row, StaffColumnMap.ColIsHonored);
        var isPrimary = ExcelFieldReader.GetOptionalBool(row, StaffColumnMap.ColIsPrimary);
        var hasMilitary = ExcelFieldReader.GetOptionalBool(row, StaffColumnMap.ColHasMilitary);
        var hasUnfavorable = ExcelFieldReader.GetOptionalBool(row, StaffColumnMap.ColHasUnfavorable);
        var disinfectants = ExcelFieldReader.GetOptionalBool(row, StaffColumnMap.ColDisinfectants);
        var nightShifts = ExcelFieldReader.GetOptionalBool(row, StaffColumnMap.ColNightShifts);

        // ─── ГПД/ПКР: Grade як int? (не призначено = null), Hours як decimal default 0 ───
        var gpdGrade = ExcelFieldReader.GetOptionalInt(row, StaffColumnMap.ColGpdGrade, "GpdGrade", rowNumber, errors);
        var gpdRate = ExcelFieldReader.GetOptionalHours(row, StaffColumnMap.ColGpdRate, "GpdRate", rowNumber, errors);
        var pkrGrade = ExcelFieldReader.GetOptionalInt(row, StaffColumnMap.ColPkrGrade, "PkrGrade", rowNumber, errors);
        var pkrHours = ExcelFieldReader.GetOptionalHours(row, StaffColumnMap.ColPkrHours, "PkrHours", rowNumber, errors);

        // ─── Окремі надбавки сумою (decimal? — null = не призначено, 0 ≠ null) ───
        var mentorAmount = ExcelFieldReader.GetOptionalDecimal(row, StaffColumnMap.ColMentorAmount, "MentorAmount", rowNumber, errors);
        var libraryMgmtAmount = ExcelFieldReader.GetOptionalDecimal(row, StaffColumnMap.ColLibraryMgmtAmount, "LibraryMgmtAmount", rowNumber, errors);
        var textbooksAmount = ExcelFieldReader.GetOptionalDecimal(row, StaffColumnMap.ColTextbooksAmount, "TextbooksAmount", rowNumber, errors);
        var directorPct = ExcelFieldReader.GetOptionalDecimal(row, StaffColumnMap.ColDirectorPct, "DirectorPct", rowNumber, errors);

        return new StaffRowDto
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
            Position = position,
            PositionStartDate = positionStartDate,
            TariffGrade = tariffGrade,
            Stavki = stavki,
            IsPrimary = isPrimary,
            HasMilitary = hasMilitary,
            HasUnfavorable = hasUnfavorable,
            GpdGrade = gpdGrade,
            GpdRate = gpdRate,
            PkrGrade = pkrGrade,
            PkrHours = pkrHours,
            MentorAmount = mentorAmount,
            LibraryMgmtAmount = libraryMgmtAmount,
            TextbooksAmount = textbooksAmount,
            Disinfectants = disinfectants,
            NightShifts = nightShifts,
            DirectorPct = directorPct,
        };
    }
}
