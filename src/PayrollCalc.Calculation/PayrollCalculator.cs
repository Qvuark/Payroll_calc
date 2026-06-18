using System.Globalization;
using PayrollCalc.Calculation.Calculators;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Interfaces;

namespace PayrollCalc.Calculation;

/// <summary>
/// Рушій розрахунку зарплати. Чистий потік вхід→вихід без БД та Excel:
/// нарахування → gross → утримання (податки) → сума до видачі.
/// Нарахування рахуються по кожній ставці окремо (~24 калькулятори: оклад, №1749, звання,
/// вислуга, престиж...), далі доплата до МЗП, ручні суми й податки.
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
            TaxId = input.TaxId,
            Year = input.Year,
            Month = input.Month,
            NormDays = input.NormDays,
            WorkedDays = input.WorkedDays,
            SocialBenefitPct = input.SocialBenefitPct,
            Positions = input.Positions,
            Earnings = earnings,
            Gross = gross,
            Deductions = deductions,
            TotalWithheld = withheld,
            NetPay = gross - withheld,
            ParamsSnapshot = Snapshot(input.Params),
        };
    }
    /// <summary>
    /// Нарахування: по кожній ставці оклад + надбавки (тег класом для J/N), потім доплата до МЗП
    /// (лише Class 3/4) і ручні суми на працівника.
    /// </summary>
    private static List<CalcComponent> BuildEarnings(CalcInput input)
    {
        var list = new List<CalcComponent>();
        // Обчислювані надбавки — окремо по кожній ставці працівника.
        // Порядок важливий: спершу оклад, далі надбавки що залежать від нього (№1749 тощо).
        foreach (var pos in input.Positions)
        {
            // Збираємо надбавки ставки окремо, щоб у кінці позначити їх класом (для J/N-колонок відомості).
            var posList = new List<CalcComponent>();

            var oklad = OkladCalculator.Calc(pos, input.NormDays, input.WorkedDays);
            posList.Add(oklad);

            var bonus1749 = Bonus1749Calculator.Calc(pos, oklad.Amount, input.Params.Bonus1749);
            AddIfAny(posList, bonus1749);

            var title = TitleCalculator.Calc(pos, oklad.Amount);
            AddIfAny(posList, title);

            // Оклад з підвищенням — база похідних надбавок (вислуга, престиж...): оклад + №1749 + звання.
            var raisedBase = oklad.Amount + (bonus1749?.Amount ?? 0m) + (title?.Amount ?? 0m);
            AddIfAny(posList, TenureCalculator.Calc(pos, raisedBase));
            AddIfAny(posList, PrestigeCalculator.Calc(pos, raisedBase));

            var rate = input.Params.Bonus1749;
            AddIfAny(posList, ClassManagementCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));
            AddIfAny(posList, CabinetCalculator.Calc(pos, rate));
            AddIfAny(posList, ComputerMaintenanceCalculator.Calc(pos, rate));
            AddIfAny(posList, WebsiteCalculator.Calc(pos, rate));
            AddIfAny(posList, MentorCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));
            AddIfAny(posList, MilitaryRecordCalculator.Calc(pos, input.NormDays, input.WorkedDays));
            AddIfAny(posList, DisinfectantsCalculator.Calc(pos, input.Params.Disinfectants));
            AddIfAny(posList, ComplexityCalculator.Calc(pos));

            // Роль-специфічні надбавки від обчисленого окладу (бібліотекар/медсестра) + нічні (сторож).
            AddIfAny(posList, LibrarianTenureCalculator.Calc(pos, oklad.Amount));
            AddIfAny(posList, LibraryHeadCalculator.Calc(pos, oklad.Amount));
            AddIfAny(posList, TextbooksCalculator.Calc(pos, oklad.Amount));
            AddIfAny(posList, MedicTenureCalculator.Calc(pos, oklad.Amount));
            AddIfAny(posList, NightShiftCalculator.Calc(pos, input.Params.NightShifts));

            AddIfAny(posList, NotebookCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));
            AddIfAny(posList, Unfavorable2600Calculator.Calc(pos, input.Params.UnfavorableBase, input.NormDays, input.WorkedDays));
            AddIfAny(posList, ReplacementCalculator.Calc(pos, rate));
            AddIfAny(posList, InclusiveCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));

            // Позаурочна робота (ГПД/ПКР) — власний блок із кількох надбавок.
            posList.AddRange(ExtendedActivityCalculator.Calc(pos, rate, input.NormDays, input.WorkedDays));

            // Позначаємо всі надбавки ставки її класом — відомість за ним обирає J- чи N-колонку.
            list.AddRange(posList.Select(c => c with { SourceClass = pos.WorkerClass }));
        }

        // Індексація — ручна сума, але кладеться ДО доплати до МЗП: за правилом вона
        // зараховується в базу мінімалки (оклад+вислуга+індексація), решта ручних — ні.
        var m = input.Manual;
        AddManual(list, ComponentNames.Indexation, m.Indexation);

        // Доплата до МЗП — лише спеціалістам і МОП (Class 3/4); педагогам/адмін-педам не належить.
        var eligibleForMinimum = input.Positions.All(p => p.WorkerClass is WorkerClass.Specialist or WorkerClass.MOP);
        if (eligibleForMinimum)
        {
            // У мінімалку зараховуються оклад/вислуга/індексація; нічні й дезінфектанти платяться ПОНАД неї.
            var countedEarnings = list
                .Where(c => c.Name is not (ComponentNames.Disinfectants or ComponentNames.NightShift))
                .Sum(c => c.Amount);
            AddIfAny(list, MinimumWageCalculator.Calc(
                input.Params.Mzp, input.Positions, countedEarnings, input.NormDays, input.WorkedDays));
        }

        // Мануальні суми — на працівника за місяць (не на ставку).
        AddManual(list, ComponentNames.Bonus, m.Bonus);
        AddManual(list, ComponentNames.Vacation, m.Vacation);
        AddManual(list, ComponentNames.SickEmployer, m.SickEmployer);
        AddManual(list, ComponentNames.SickFss, m.SickFss);
        AddManual(list, ComponentNames.Recalculation, m.Recalculation);
        AddManual(list, ComponentNames.Holiday, m.Holiday);
        AddManual(list, ComponentNames.AnnualBonus, m.AnnualBonus);
        AddManual(list, ComponentNames.PhysEducation, m.PhysEducation);
        AddManual(list, ComponentNames.VacationCompensation, m.VacationCompensation);
        AddManual(list, ComponentNames.Downtime, m.Downtime);
        AddManual(list, ComponentNames.Courses, m.Courses);
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
            new(ComponentNames.Pdfo, gross * p.Pdfo, Pct(gross, p.Pdfo)),
            new(ComponentNames.Vz, gross * p.Vz, Pct(gross, p.Vz)),
        };

        var unionBase = gross - m.SickFss;
        var unionFormula = m.SickFss != 0
            ? $"=({Num(gross)}-{Num(m.SickFss)})*{Num(p.Union * 100)}%"
            : Pct(gross, p.Union);
        list.Add(new CalcComponent(ComponentNames.UnionFee, unionBase * p.Union, unionFormula));

        AddManual(list, ComponentNames.Advance, m.Advance);
        AddManual(list, ComponentNames.EnforcementOrders, m.EnforcementOrders);
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
    /// Число у формулі — крапка-роздільник (InvariantCulture) і без хвостових нулів
    /// ("53171.2125", "18", а не "53171.21250", "18.00"), щоб формула була чиста.
    /// </summary>
    private static string Num(decimal value)
        => value.ToString("0.############", CultureInfo.InvariantCulture);
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
