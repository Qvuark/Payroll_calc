using ClosedXML.Excel;
using PayrollCalc.Core.DTOs.Calculation;

namespace PayrollCalc.Documents.Export.Vedomost;

/// <summary>
/// Будує розрахунково-платіжну відомість (лист 1 еталона) з результатів рушія.
/// Кожна надбавка лягає у свою колонку як формула-літерал; у багатоставкового працівника
/// надбавки однієї колонки додаються (напр. престижність директора+вчителя). Підсумки (BC/податки/
/// до видачі) і рядок «Разом» — живі формули, щоб правка клітинки одразу перераховувала суми.
/// </summary>
public sealed class VedomostExporter
{
    private const string Font = "Times New Roman";
    private const string MoneyFormat = "0.00";

    public byte[] Build(IReadOnlyList<CalcResult> results, int year, int month)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("ведомость");

        WriteTitle(ws, year, month);
        WriteHeader(ws);

        const int firstDataRow = 3;
        var unmapped = new SortedSet<string>();
        for (var i = 0; i < results.Count; i++)
            WriteEmployeeRow(ws, results[i], i + 1, firstDataRow + i, unmapped);

        // Невідома надбавка без колонки = мовчазна втрата в gross. Кидаємо, щоб помітити одразу.
        if (unmapped.Count > 0)
            throw new InvalidOperationException(
                "Надбавки без колонки відомості: " + string.Join(", ", unmapped));

        var lastDataRow = firstDataRow + results.Count - 1;
        WriteTotals(ws, firstDataRow, lastDataRow);
        StyleGrid(ws, lastDataRow + 1);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void WriteTitle(IXLWorksheet ws, int year, int month)
    {
        var cell = ws.Cell("B1");
        cell.Value = $"Розрахунково платіжна відомість нарахування зарплати за {ExportText.MonthUk(month)} {year}р.";
        cell.Style.Font.FontName = Font;
        cell.Style.Font.FontSize = 12;
        cell.Style.Font.Bold = true;
    }

    private static void WriteHeader(IXLWorksheet ws)
    {
        foreach (var col in VedomostLayout.Columns)
            ws.Cell($"{col.Letter}2").Value = col.Header;
    }

    private static void WriteEmployeeRow(
        IXLWorksheet ws, CalcResult r, int number, int row, ISet<string> unmapped)
    {
        ws.Cell($"A{row}").Value = number;
        ws.Cell($"B{row}").Value = r.FullName;
        ws.Cell($"C{row}").Value = r.WorkedDays;

        // Збираємо формули по колонках: одна колонка може отримати кілька надбавок (багатоставковий).
        var byColumn = new Dictionary<string, List<string>>();
        foreach (var c in r.Earnings)
        {
            var col = VedomostLayout.EarningColumn(c.Name, c.SourceClass);
            if (col is null)
                unmapped.Add(c.Name);
            else
                Accumulate(byColumn, col, c.Formula);
        }
        foreach (var d in r.Deductions)
        {
            var col = VedomostLayout.DeductionColumn(d.Name);
            if (col is not null)
                Accumulate(byColumn, col, d.Formula);
        }
        foreach (var (col, formulas) in byColumn)
            ws.Cell($"{col}{row}").FormulaA1 = string.Join("+", formulas);

        WriteLiveTotals(ws, r, row);
    }

    /// <summary>
    /// Живі підсумкові формули рядка: gross, податки, профспілка, всього утримано, до видачі.
    /// Відсотки податків беремо зі знімка параметрів цього розрахунку (а не хардкод).
    /// </summary>
    private static void WriteLiveTotals(IXLWorksheet ws, CalcResult r, int row)
    {
        var pdfo = Pct(r.ParamsSnapshot, "pdfo");
        var vz = Pct(r.ParamsSnapshot, "vz");
        var union = Pct(r.ParamsSnapshot, "union");

        ws.Cell($"BC{row}").FormulaA1 = $"SUM(J{row}:BB{row})";
        ws.Cell($"BD{row}").FormulaA1 = $"BC{row}*{pdfo}%";
        ws.Cell($"BE{row}").FormulaA1 = $"BC{row}*{vz}%";
        // Профспілка береться з gross мінус лікарняні ФСС (колонка AM) — як в еталоні.
        ws.Cell($"BF{row}").FormulaA1 = $"BC{row}*{union}%-AM{row}*{union}%";
        ws.Cell($"BJ{row}").FormulaA1 = $"BD{row}+BE{row}+BF{row}+BG{row}+BI{row}";
        ws.Cell($"BK{row}").FormulaA1 = $"BC{row}-BJ{row}";
    }

    private static void WriteTotals(IXLWorksheet ws, int firstDataRow, int lastDataRow)
    {
        var row = lastDataRow + 1;
        ws.Cell($"B{row}").Value = "Разом";
        foreach (var col in VedomostLayout.Columns.Where(c => c.Money))
            ws.Cell($"{col.Letter}{row}").FormulaA1 = $"SUM({col.Letter}{firstDataRow}:{col.Letter}{lastDataRow})";
        ws.Row(row).Style.Font.Bold = true;
    }

    private static void StyleGrid(IXLWorksheet ws, int lastRow)
    {
        // Сітка: Times New Roman 9 + тонкі рамки на всю таблицю (шапка + дані + Разом).
        var grid = ws.Range($"A2:BK{lastRow}");
        grid.Style.Font.FontName = Font;
        grid.Style.Font.FontSize = 9;
        grid.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        grid.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Шапка — вертикальний текст із переносом, як в еталоні.
        var header = ws.Range("A2:BK2");
        header.Style.Alignment.TextRotation = 90;
        header.Style.Alignment.WrapText = true;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(2).Height = 130;

        // Грошові колонки — формат 0.00.
        foreach (var col in VedomostLayout.Columns.Where(c => c.Money))
            ws.Range($"{col.Letter}3:{col.Letter}{lastRow}").Style.NumberFormat.Format = MoneyFormat;

        // Колонка № — світло-зелена заливка як в еталоні.
        ws.Range($"A3:A{lastRow - 1}").Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFD9");

        ws.Column("A").Width = 4;
        ws.Column("B").Width = 22;
    }

    private static void Accumulate(Dictionary<string, List<string>> map, string col, string formula)
    {
        if (!map.TryGetValue(col, out var list))
            map[col] = list = [];
        list.Add(formula.TrimStart('='));
    }

    /// <summary>
    /// Відсоток зі знімка (0.18 → "18", 0.05 → "5") для тексту формули "BC*18%".
    /// </summary>
    private static string Pct(IReadOnlyDictionary<string, decimal> snapshot, string key)
        => (snapshot.TryGetValue(key, out var v) ? v * 100m : 0m)
            .ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
