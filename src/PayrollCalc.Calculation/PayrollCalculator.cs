using System.Globalization;
using PayrollCalc.Calculation.Calculators;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Interfaces;

namespace PayrollCalc.Calculation;

/// <summary>
/// Рушій розрахунку зарплати. Чистий потік вхід→вихід без БД та Excel:
/// нарахування → gross → утримання (податки) → сума до видачі.
/// Обчислювані надбавки (оклад, +40%, звання, вислуга...) додаються калькуляторами далі;
/// поки рушій обробляє лише мануальні суми та податки.
/// </summary>
public sealed class PayrollCalculator : IPayrollCalculator
{
    public CalcResult Calculate(CalcInput input)
    {
        var earnings = BuildEarnings(input);
        var gross = earnings.Sum(c => c.Amount);

        var deductions = BuildDeductions(input, gross);
        var withheld = deductions.Sum(c => c.Amount);

        return new CalcResult
        {
            EmployeeId = input.EmployeeId,
            FullName = input.FullName,
            Year = input.Year,
            Month = input.Month,
            Earnings = earnings,
            Gross = gross,
            Deductions = deductions,
            TotalWithheld = withheld,
            NetPay = gross - withheld,
            ParamsSnapshot = Snapshot(input.Params),
        };
    }
    /// <summary>
    /// Нарахування. Поки лише мануальні суми; обчислювані надбавки додаються наступними кроками.
    /// </summary>
    private static List<CalcComponent> BuildEarnings(CalcInput input)
    {
        var list = new List<CalcComponent>();

        // Обчислювані надбавки — окремо по кожній ставці працівника.
        // Порядок важливий: спершу оклад, далі надбавки що залежать від нього (№1749 тощо).
        foreach (var pos in input.Positions)
        {
            var oklad = OkladCalculator.Calc(pos, input.NormDays, input.WorkedDays);
            list.Add(oklad);

            var bonus1749 = Bonus1749Calculator.Calc(pos, oklad.Amount, input.Params.Bonus1749);
            AddIfAny(list, bonus1749);

            var title = TitleCalculator.Calc(pos, oklad.Amount);
            AddIfAny(list, title);

            // Оклад з підвищенням — база похідних надбавок (вислуга, престиж...): оклад + №1749 + звання.
            var raisedBase = oklad.Amount + (bonus1749?.Amount ?? 0m) + (title?.Amount ?? 0m);
            AddIfAny(list, TenureCalculator.Calc(pos, raisedBase));
            AddIfAny(list, PrestigeCalculator.Calc(pos, raisedBase));

            var rate = input.Params.Bonus1749;
            AddIfAny(list, ClassManagementCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));
            AddIfAny(list, CabinetCalculator.Calc(pos, rate));
            AddIfAny(list, ComputerMaintenanceCalculator.Calc(pos, rate));
            AddIfAny(list, WebsiteCalculator.Calc(pos, rate));
            AddIfAny(list, MentorCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));
            AddIfAny(list, MilitaryRecordCalculator.Calc(pos, input.NormDays, input.WorkedDays));
            AddIfAny(list, DisinfectantsCalculator.Calc(pos, input.Params.Disinfectants));
            AddIfAny(list, ComplexityCalculator.Calc(pos));

            // Роль-специфічні надбавки від обчисленого окладу (бібліотекар/медсестра) + нічні (сторож).
            AddIfAny(list, LibrarianTenureCalculator.Calc(pos, oklad.Amount));
            AddIfAny(list, LibraryHeadCalculator.Calc(pos, oklad.Amount));
            AddIfAny(list, TextbooksCalculator.Calc(pos, oklad.Amount));
            AddIfAny(list, MedicTenureCalculator.Calc(pos, oklad.Amount));
            AddIfAny(list, NightShiftCalculator.Calc(pos, input.Params.NightShifts));

            AddIfAny(list, NotebookCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));
            AddIfAny(list, Unfavorable2600Calculator.Calc(pos, input.Params.UnfavorableBase, input.NormDays, input.WorkedDays));
            AddIfAny(list, ReplacementCalculator.Calc(pos));
            AddIfAny(list, InclusiveCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));
        }

        // Доплата до МЗП — лише спеціалістам і МОП (Class 3/4); педагогам/адмін-педам не належить.
        var eligibleForMinimum = input.Positions.All(p => p.WorkerClass is WorkerClass.Specialist or WorkerClass.MOP);
        if (eligibleForMinimum)
        {
            // У мінімалку зараховуються оклад/вислуга/індексація; нічні й дезінфектанти платяться ПОНАД неї.
            var countedEarnings = list
                .Where(c => c.Name is not ("Дезінфікуючі засоби" or "Доплата за нічні"))
                .Sum(c => c.Amount);
            AddIfAny(list, MinimumWageCalculator.Calc(
                input.Params.Mzp, input.Positions, countedEarnings, input.NormDays, input.WorkedDays));
        }

        // Мануальні суми — на працівника за місяць (не на ставку).
        var m = input.Manual;
        AddManual(list, "Премія", m.Bonus);
        AddManual(list, "Відпускні", m.Vacation);
        AddManual(list, "Лікарняні (роботодавець)", m.SickEmployer);
        AddManual(list, "Лікарняні (ФСС)", m.SickFss);
        AddManual(list, "Перерахунок", m.Recalculation);
        return list;
    }
    /// <summary>
    /// Утримання: податки (ПДФО, військовий збір, профспілка) + мануальні (аванс, виконавчі листи).
    /// База профспілки = gross мінус лікарняні ФСС.
    /// </summary>
    private static List<CalcComponent> BuildDeductions(CalcInput input, decimal gross)
    {
        var p = input.Params;
        var m = input.Manual;
        var list = new List<CalcComponent>
        {
            new("ПДФО", gross * p.Pdfo, Pct(gross, p.Pdfo)),
            new("Військовий збір", gross * p.Vz, Pct(gross, p.Vz)),
        };

        var unionBase = gross - m.SickFss;
        var unionFormula = m.SickFss != 0
            ? $"=({Num(gross)}-{Num(m.SickFss)})*{Num(p.Union * 100)}%"
            : Pct(gross, p.Union);
        list.Add(new CalcComponent("Профспілковий внесок", unionBase * p.Union, unionFormula));

        AddManual(list, "Аванс", m.Advance);
        AddManual(list, "Виконавчі листи", m.EnforcementOrders);
        return list;
    }
    /// <summary>
    /// Додає компонент від калькулятора, якщо він є (null = надбавка не застосовна до цієї ставки).
    /// </summary>
    private static void AddIfAny(List<CalcComponent> list, CalcComponent? component)
    {
        if (component is not null)
            list.Add(component);
    }

    /// <summary>
    /// Додає мануальний компонент лише якщо сума ≠ 0 (нуль = порожня клітинка відомості, не "0").
    /// </summary>
    private static void AddManual(List<CalcComponent> list, string name, decimal amount)
    {
        if (amount != 0)
            list.Add(new CalcComponent(name, amount, "=" + Num(amount)));
    }
    /// <summary>
    /// Формула "база×відсоток" з підставленими числами, напр. "=15000*18%".
    /// </summary>
    private static string Pct(decimal baseAmount, decimal rate)
        => $"={Num(baseAmount)}*{Num(rate * 100)}%";
    /// <summary>
    /// Число у формулі — завжди з крапкою (InvariantCulture), щоб Excel не плутав з комою-роздільником.
    /// </summary>
    private static string Num(decimal value)
        => value.ToString(CultureInfo.InvariantCulture);
    /// <summary>
    /// Знімок параметрів розрахунку (ключі = ключі SystemParam) для збереження й аудиту.
    /// </summary>
    private static Dictionary<string, decimal> Snapshot(PayrollParams p) => new()
    {
        ["pdfo"] = p.Pdfo,
        ["vz"] = p.Vz,
        ["union"] = p.Union,
        ["bonus_1749"] = p.Bonus1749,
        ["mzp"] = p.Mzp,
        ["unfavorable_base"] = p.UnfavorableBase,
        ["disinfectants"] = p.Disinfectants,
        ["night_shifts"] = p.NightShifts,
    };
}
