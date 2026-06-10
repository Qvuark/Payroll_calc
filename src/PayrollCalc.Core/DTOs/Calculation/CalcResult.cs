namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Результат розрахунку зарплати одного працівника за місяць.
/// Нарахування = колонки J:BB відомості (кожна надбавка окремим компонентом),
/// далі gross, утримання (податки/аванс), всього утримано і сума до видачі.
/// З цього Documents будує і відомість (числа в колонки), і розрахунковий лист (рядки з формулами).
/// </summary>
public record CalcResult
{
    public required int EmployeeId { get; init; }
    public required string FullName { get; init; }
    /// <summary>
    /// ІПН працівника (шапка розрахункового листа).
    /// </summary>
    public string TaxId { get; init; } = "";
    public required int Year { get; init; }
    public required int Month { get; init; }
    /// <summary>
    /// Норма робочих днів місяця (відомість — знаменник пропорції).
    /// </summary>
    public int NormDays { get; init; }
    /// <summary>
    /// Відпрацьовано днів (відомість C). За замовчуванням = норма.
    /// </summary>
    public decimal WorkedDays { get; init; }
    /// <summary>
    /// Ставки працівника (посада, клас, навантаження) — для шапки розрахункового листа.
    /// </summary>
    public IReadOnlyList<PositionCalcInput> Positions { get; init; } = [];
    /// <summary>
    /// Відсоток податкової соц.пільги — для шапки розрахункового листа. Null = пільги немає.
    /// </summary>
    public decimal? SocialBenefitPct { get; init; }
    /// <summary>
    /// Нарахування — кожна надбавка окремим компонентом (відповідає колонкам J:BB відомості).
    /// </summary>
    public required IReadOnlyList<CalcComponent> Earnings { get; init; }
    /// <summary>
    /// Сума нарахувань (gross). Відповідає BC = SUM(J:BB).
    /// </summary>
    public required decimal Gross { get; init; }
    /// <summary>
    /// Утримання: ПДФО, військовий збір, профспілка, аванс, виконавчі листи тощо.
    /// </summary>
    public required IReadOnlyList<CalcComponent> Deductions { get; init; }
    /// <summary>
    /// Всього утримано (BJ = сума утримань + аванс).
    /// </summary>
    public required decimal TotalWithheld { get; init; }
    /// <summary>
    /// Сума до видачі (BK = gross − всього утримано).
    /// </summary>
    public required decimal NetPay { get; init; }
    /// <summary>
    /// Знімок системних параметрів, з якими рахували (ключ SystemParam → значення).
    /// Зберігається при кожному розрахунку для відтворюваності й аудиту.
    /// </summary>
    public required IReadOnlyDictionary<string, decimal> ParamsSnapshot { get; init; }
}
