using ClosedXML.Excel;
using PayrollCalc.Core.DTOs.Calculation;

namespace PayrollCalc.Documents.Export.Payslip;

/// <summary>
/// Будує лист розрахункових листів (лист 2 еталона): по дві платіжки в ряд (ліва B-E, права F-I),
/// блоками згори вниз. Кожна платіжка — шапка (ПІБ/ІПН/посада/дні), секції «Нараховано» та
/// «Утримано» з формулами надбавок, і підсумки «Всього…/Сума до видачі» живими формулами.
/// Висота блоку динамічна — за більшою з двох платіжок.
/// </summary>
public sealed class PayslipExporter
{
    private const string Font = "Times New Roman";

    public byte[] Build(IReadOnlyList<CalcResult> results, int year, int month)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("розрахунковий лист");
        ws.Style.Font.FontName = Font;
        ws.Style.Font.FontSize = 9;

        var row = 1;
        for (var i = 0; i < results.Count; i += 2)
        {
            var leftEnd = RenderPayslip(ws, results[i], row, colBase: 2, year, month);
            var rightEnd = i + 1 < results.Count
                ? RenderPayslip(ws, results[i + 1], row, colBase: 6, year, month)
                : leftEnd;
            row = Math.Max(leftEnd, rightEnd) + 2;   // порожній рядок між блоками
        }

        StyleSheet(ws, row);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <param name="colBase">Колонка мітки нарахування (ліва платіжка = B/2, права = F/6).</param>
    /// <returns>Останній зайнятий рядок платіжки.</returns>
    private static int RenderPayslip(IXLWorksheet ws, CalcResult r, int startRow, int colBase, int year, int month)
    {
        int el = colBase, ev = colBase + 1, dl = colBase + 2, dv = colBase + 3;
        var evL = XLHelper.GetColumnLetterFromNumber(ev);
        var dvL = XLHelper.GetColumnLetterFromNumber(dv);

        var row = startRow;
        Bold(ws.Cell(row, el)).Value = $"Розрахунковий лист за {ExportText.MonthUk(month)} {year}р.";
        row++;
        Bold(ws.Cell(row, el)).Value = r.FullName;
        row++;
        ws.Cell(row, el).Value = $"ІПН {r.TaxId}";
        ws.Cell(row, ev).Value = "посада:";
        ws.Cell(row, dl).Value = string.Join(", ", r.Positions.Select(p => p.PositionName));
        row++;
        ws.Cell(row, el).Value = "Відпрацьовано днів/норма";
        ws.Cell(row, ev).Value = $"{r.WorkedDays}/{r.NormDays}";
        row++;
        var pedHours = r.Positions.Sum(p => p.PedHoursWeekly);
        if (pedHours > 0)
        {
            ws.Cell(row, el).Value = $"Тижневе пед.навантаження {pedHours} год.";
            row++;
        }

        Bold(ws.Cell(row, el)).Value = "Нараховано";
        Bold(ws.Cell(row, dl)).Value = "Утримано";
        row++;

        // Дві колонки незалежні: нарахування вниз по el/ev, утримання вниз по dl/dv.
        var firstItemRow = row;
        var er = row;
        foreach (var c in r.Earnings)
        {
            ws.Cell(er, el).Value = c.Name;
            ws.Cell(er, ev).FormulaA1 = c.Formula.TrimStart('=');
            er++;
        }
        var dr = row;
        foreach (var d in r.Deductions)
        {
            ws.Cell(dr, dl).Value = d.Name;
            ws.Cell(dr, dv).FormulaA1 = d.Formula.TrimStart('=');
            dr++;
        }

        var totalRow = Math.Max(er, dr);
        Bold(ws.Cell(totalRow, el)).Value = "Всього нараховано";
        ws.Cell(totalRow, ev).FormulaA1 = er > firstItemRow ? $"SUM({evL}{firstItemRow}:{evL}{er - 1})" : "0";
        Bold(ws.Cell(totalRow, dl)).Value = "Всього утримано";
        ws.Cell(totalRow, dv).FormulaA1 = dr > firstItemRow ? $"SUM({dvL}{firstItemRow}:{dvL}{dr - 1})" : "0";

        var payRow = totalRow + 1;
        Bold(ws.Cell(payRow, el)).Value = "Сума до видачі";
        ws.Cell(payRow, ev).FormulaA1 = $"{evL}{totalRow}-{dvL}{totalRow}";

        ws.Range(startRow, el, payRow, dv).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        return payRow;
    }

    private static void StyleSheet(IXLWorksheet ws, int lastRow)
    {
        // Грошові (значення) колонки платіжок — формат 0.00.
        foreach (var c in new[] { "C", "E", "G", "I" })
            ws.Range($"{c}1:{c}{lastRow}").Style.NumberFormat.Format = "0.00";

        ws.Column("A").Width = 3;
        foreach (var c in new[] { "B", "D", "F", "H" })   // мітки
            ws.Column(c).Width = 30;
        foreach (var c in new[] { "C", "E", "G", "I" })   // значення
            ws.Column(c).Width = 13;
    }

    private static IXLCell Bold(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        return cell;
    }
}
