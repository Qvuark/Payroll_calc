using System.Data;
using PayrollCalc.Documents.Import.Common;

namespace PayrollCalc.Documents.Import.Teachers;

/// <summary>
/// Парсер teachers.xlsx. Stream → (List&lt;TeachersRowDto&gt;, List&lt;ParserError&gt;).
/// Не throws на bad data — збирає всі помилки у список, бухгалтер бачить повний звіт за один прохід.
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
            // Передаємо у ParseRow саме "людський" номер, щоб бухгалтер бачив
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
    /// не throws — бухгалтер хоче бачити повний звіт за один прохід.
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
        var tabNumber = ExcelFieldReader.GetMandatoryString(row, TeachersColumnMap.ColTabNumber, "TabNumber", rowNumber, errors);
        var fullName = ExcelFieldReader.GetMandatoryString(row, TeachersColumnMap.ColFullName, "FullName", rowNumber, errors);
        var taxId = ExcelFieldReader.GetMandatoryString(row, TeachersColumnMap.ColTaxId, "TaxId", rowNumber, errors);
        var hireDate = ExcelFieldReader.GetMandatoryDate(row, TeachersColumnMap.ColHireDate, "HireDate", rowNumber, errors);
        var position = ExcelFieldReader.GetMandatoryString(row, TeachersColumnMap.ColPosition, "Position", rowNumber, errors);
        var stavki = ExcelFieldReader.GetMandatoryDecimal(row, TeachersColumnMap.ColStavki, "Stavki", rowNumber, errors);
        var tariffGrade = ExcelFieldReader.GetMandatoryInt(row, TeachersColumnMap.ColTariffGrade, "TariffGrade", rowNumber, errors);
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
        var education = ExcelFieldReader.GetOptionalString(row, TeachersColumnMap.ColEducation);
        var titleType = ExcelFieldReader.GetOptionalString(row, TeachersColumnMap.ColTitleType);
        var subject = ExcelFieldReader.GetOptionalString(row, TeachersColumnMap.ColSubject);
        var classMgmt = ExcelFieldReader.GetOptionalString(row, TeachersColumnMap.ColClassMgmt);
        var cabinetType = ExcelFieldReader.GetOptionalString(row, TeachersColumnMap.ColCabinetType);

        var positionStartDate = ExcelFieldReader.GetOptionalDate(row, TeachersColumnMap.ColPositionStartDate, "PositionStartDate", rowNumber, errors);

        var honoredAmount = ExcelFieldReader.GetOptionalDecimal(row, TeachersColumnMap.ColHonoredAmount, "HonoredAmount", rowNumber, errors);
        var socialBenefitPct = ExcelFieldReader.GetOptionalDecimal(row, TeachersColumnMap.ColSocialBenefitPct, "SocialBenefitPct", rowNumber, errors);
        var complexityPct = ExcelFieldReader.GetOptionalDecimal(row, TeachersColumnMap.ColComplexityPct, "ComplexityPct", rowNumber, errors);
        var prestigePct = ExcelFieldReader.GetOptionalDecimal(row, TeachersColumnMap.ColPrestigePct, "PrestigePct", rowNumber, errors);

        var pedExpYears = ExcelFieldReader.GetOptionalInt(row, TeachersColumnMap.ColPedExpYears, "PedExpYears", rowNumber, errors);
        var generalExpYears = ExcelFieldReader.GetOptionalInt(row, TeachersColumnMap.ColGeneralExpYears, "GeneralExpYears", rowNumber, errors);

        var isHonored = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColIsHonored);
        var isPrimary = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColIsPrimary);
        var hasMilitary = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColHasMilitary);
        var hasUnfavorable = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColHasUnfavorable);
        var gym = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColGym);
        var shooting = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColShooting);
        var computers = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColComputers);
        var extracurricular = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColExtracurricular);
        var website = ExcelFieldReader.GetOptionalBool(row, TeachersColumnMap.ColWebsite);

        // ─── Hours (decimal, default 0) ───
        var hours1To4 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColHours1To4, "Hours1To4", rowNumber, errors);
        var ind1To4 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColIndividualHours1To4, "IndividualHours1To4", rowNumber, errors);
        var hours5To9 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColHours5To9, "Hours5To9", rowNumber, errors);
        var ind5To9 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColIndividualHours5To9, "IndividualHours5To9", rowNumber, errors);
        var hours10To11 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColHours10To11, "Hours10To11", rowNumber, errors);
        var ind10To11 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColIndividualHours10To11, "IndividualHours10To11", rowNumber, errors);
        var notebook1To4 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColNotebookHours1To4, "NotebookHours1To4", rowNumber, errors);
        var notebook5To9 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColNotebookHours5To9, "NotebookHours5To9", rowNumber, errors);
        var notebook10To11 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColNotebookHours10To11, "NotebookHours10To11", rowNumber, errors);
        var inclusive1To4 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColInclusiveHours1To4, "InclusiveHours1To4", rowNumber, errors);
        var inclusive5To9 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColInclusiveHours5To9, "InclusiveHours5To9", rowNumber, errors);
        var inclusive10To11 = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColInclusiveHours10To11, "InclusiveHours10To11", rowNumber, errors);
        var additionalHours = ExcelFieldReader.GetOptionalHours(row, TeachersColumnMap.ColAdditionalHours, "AdditionalHours", rowNumber, errors);

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
            AdditionalHours = additionalHours,
            ClassMgmt = classMgmt,
            CabinetType = cabinetType,
            Gym = gym,
            Shooting = shooting,
            Computers = computers,
            Extracurricular = extracurricular,
            Website = website,
        };
    }
}
