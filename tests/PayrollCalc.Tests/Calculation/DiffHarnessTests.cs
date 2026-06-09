using FluentAssertions;
using PayrollCalc.Calculation;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;
using Xunit.Abstractions;

namespace PayrollCalc.Tests.Calculation;

/// <summary>
/// Diff-харнес: будуємо вхід архетипів із реальної відомості `для клода.xlsx` (березень 2026)
/// і звіряємо результат рушія з її клітинками. Інструмент розробки — показує що збігається й що
/// лишилось (хвіст S/U/AW/AY). Не частина програми.
/// </summary>
public class DiffHarnessTests(ITestOutputHelper output)
{
    private static readonly PayrollParams Params = new()
    {
        Pdfo = 0.18m, Vz = 0.05m, Union = 0.01m, Bonus1749 = 0.40m,
        Mzp = 8647m, UnfavorableBase = 2600m, Disinfectants = 0.10m, NightShifts = 0.40m,
    };

    [Fact]
    public void Medic_r57_GrossMatchesEtalon()
    {
        // Суховіцька: спеціаліст, оклад 6003, вислуга мед 30%, доплата до МЗП. Еталон BC=8647.
        var pos = Spec(6003m) with { HasMedicTenure = true };
        var result = Calc(pos);

        Component(result, "Вислуга медпрацівника").Should().Be(1800.9m);
        Component(result, "Доплата до МЗП").Should().Be(843.1m);
        result.Gross.Should().Be(8647m);
    }

    [Fact]
    public void Librarian_r58_GrossMatchesEtalon()
    {
        // Костенко: бібліотекар, оклад 7356. V=30%, W=50%, X=8%. Еталон BC=13829.28.
        var pos = Spec(7356m) with { HasLibrarianTenure = true, IsLibraryHead = true, HasTextbooks = true };
        var result = Calc(pos);

        Component(result, "Вислуга бібліотекаря").Should().Be(2206.8m);
        Component(result, "Завідування бібліотекою").Should().Be(3678m);
        Component(result, "За підручники").Should().Be(588.48m);
        result.Gross.Should().Be(13829.28m);
    }

    [Fact]
    public void Guard_r76_GrossMatchesEtalon()
    {
        // Шамрай: сторож погодинний, тариф 3782, 187 год, 126 нічних. Еталон BC=10270.46.
        var pos = Mop(3782m) with { IsHourly = true, WorkedHours = 187m, NightHours = 126m };
        var result = Calc(pos);

        result.Gross.Should().BeApproximately(10270.4648m, 0.001m);
    }

    [Fact]
    public void Skyrda_r3_CoreMatches_TailDocumented()
    {
        // Скирда: директор(J)+вчитель(N). Ядро (оклад/1749/звання/вислуга/престиж/складність/премія) має збігтись.
        // Хвіст НЕ реалізований: S зошити 867.69 + U інклюзив 2914.8 + AW заміни 2049.6 + AY 2600 3900 = 9732.09.
        var director = new PositionCalcInput
        {
            WorkerClass = WorkerClass.AdminPedagogical, PositionName = "Директор",
            Oklad = 10410m, RateCount = 1m, TenurePct = 0.30m, PrestigePct = 0.25m, ComplexityPct = 0.50m,
        };
        var teacher = new PositionCalcInput
        {
            WorkerClass = WorkerClass.Pedagogical, PositionName = "Вчитель",
            Oklad = 8397m, RateCount = 1m, PedHoursWeekly = 9m, TitlePct = 0.15m, TenurePct = 0.30m, PrestigePct = 0.20m,
            NotebookHours = 8m, NotebookPct = 0.15m,                          // S зошити
            HasUnfavorable2600 = true,                                       // AY 2600
            ReplacementRate = 256.2m, ReplacementHours = 8m,                 // AW заміни
        };
        var result = Calc(new ManualAdjustments { Bonus = 15615m }, director, teacher);

        // Престижність = (J+K)×25% + (N+O+P)×20% — головна перевірка по-позиційної логіки.
        Component(result, "Престижність").Should().Be(4945.035m);
        Component(result, "Складність і напруженість").Should().Be(5205m);
        Component(result, "За перевірку зошитів").Should().Be(867.69m);      // S
        Component(result, "Несприятливі умови (2600)").Should().Be(3900m);   // AY
        Component(result, "Заміни").Should().Be(2049.6m);                    // AW

        // Лишається тільки U інклюзив 2914.8 — аномальний (директорський варіант (J+K)×20%), на diff-тюнінг.
        const decimal etalonGross = 62903.3025m;
        const decimal inclusiveU = 2914.8m;
        result.Gross.Should().Be(etalonGross - inclusiveU);                  // 59988.5025

        output.WriteLine($"Скирда: рушій={result.Gross}, еталон={etalonGross}, лишилось U(інклюзив)={inclusiveU}");
    }

    [Fact]
    public void Vdovchenko_r4_InclusiveMatchesEtalon()
    {
        // Вдовченко r4: вчитель, неповний місяць 17/22, інклюзив G=3.5.
        // U = (8397+1749+звання15%)×20%/18×3.5 /22×17 = 391.1178.
        var teacher = new PositionCalcInput
        {
            WorkerClass = WorkerClass.Pedagogical, PositionName = "Вчитель",
            Oklad = 8397m, RateCount = 1m, PedHoursWeekly = 9m, TitlePct = 0.15m, InclusiveHours = 3.5m,
        };
        var result = new PayrollCalculator().Calculate(new CalcInput
        {
            EmployeeId = 1, FullName = "Вдовченко", Year = 2026, Month = 3,
            NormDays = 22, WorkedDays = 17, Positions = [teacher], Manual = new ManualAdjustments(), Params = Params,
        });

        Component(result, "Інклюзивні класи").Should().BeApproximately(391.1178m, 0.001m);
    }

    [Fact]
    public void Pkr_r20_BlockMatchesEtalon()
    {
        // Сухенко: ПКР AF=6836/18*2, AG=×40%, AH=(AF+AG)×30%, AI=(AF+AG)×20%.
        var pos = Teacher() with
        {
            ExtendedActivity = new ExtendedActivityInput
            {
                Kind = ExtendedActivityKind.Pkr, Tariff = 6836m, Divisor = 18m, Hours = 2m,
                TenurePct = 0.30m, ProrateByDays = true,
            },
        };
        var result = Calc(pos);

        Component(result, "За ПКР").Should().BeApproximately(759.5556m, 0.001m);
        Component(result, "Підвищення №22").Should().BeApproximately(303.8222m, 0.001m);
        Component(result, "Вислуга ГПД/ПКР").Should().BeApproximately(319.0133m, 0.001m);
        Component(result, "Престижність ГПД/ПКР").Should().BeApproximately(212.6756m, 0.001m);
    }

    [Fact]
    public void Gpd_r27_BlockMatchesEtalon()
    {
        // Троцюк: ГПД AE=8397/4, AG=×40%, AH=(AE+AG)×30%, AI=(AE+AG)×20%.
        var pos = Teacher() with
        {
            ExtendedActivity = new ExtendedActivityInput
            {
                Kind = ExtendedActivityKind.Gpd, Tariff = 8397m, Divisor = 4m, Hours = 1m,
                TenurePct = 0.30m, ProrateByDays = false,
            },
        };
        var result = Calc(pos);

        Component(result, "За ГПД").Should().Be(2099.25m);
        Component(result, "Підвищення №22").Should().Be(839.70m);
        Component(result, "Вислуга ГПД/ПКР").Should().Be(881.685m);
        Component(result, "Престижність ГПД/ПКР").Should().Be(587.79m);
    }

    // --- helpers ---

    // Гола педставка без навантаження: оклад=0, лишаються тільки надбавки блоку що перевіряємо.
    private static PositionCalcInput Teacher() => new()
    {
        WorkerClass = WorkerClass.Pedagogical, PositionName = "Вчитель", Oklad = 8397m, RateCount = 1m,
    };

    private static CalcResult Calc(params PositionCalcInput[] positions) => Calc(new ManualAdjustments(), positions);

    private static CalcResult Calc(ManualAdjustments manual, params PositionCalcInput[] positions)
        => new PayrollCalculator().Calculate(new CalcInput
        {
            EmployeeId = 1, FullName = "Архетип", Year = 2026, Month = 3,
            NormDays = 22, WorkedDays = 22, Positions = positions, Manual = manual, Params = Params,
        });

    // Сумуємо по імені: у багатоставкового (Скирда) одна надбавка може бути на кожній ставці.
    private static decimal Component(CalcResult r, string name) => r.Earnings.Where(e => e.Name == name).Sum(e => e.Amount);

    private static PositionCalcInput Spec(decimal oklad) => new()
    {
        WorkerClass = WorkerClass.Specialist, PositionName = "Спеціаліст", Oklad = oklad, RateCount = 1m,
    };

    private static PositionCalcInput Mop(decimal oklad) => new()
    {
        WorkerClass = WorkerClass.MOP, PositionName = "МОП", Oklad = oklad, RateCount = 1m,
    };
}
