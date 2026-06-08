using FluentAssertions;
using PayrollCalc.Calculation;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Tests.Calculation;

/// <summary>
/// Юніт-тести рушія (без БД): оклад, пропорція за дні, №1749, податки, мануальні суми.
/// Числа звірені вручну з еталоном (відомість). Податки/мануал перевіряємо на МОП —
/// у нього нема надбавок, тому gross чистий і числа не "поїдуть" з додаванням нових калькуляторів.
/// </summary>
public class PayrollCalculatorTests
{
    private static readonly PayrollParams DefaultParams = new()
    {
        Pdfo = 0.18m,
        Vz = 0.05m,
        Union = 0.01m,
        Bonus1749 = 0.40m,
        Mzp = 8647m,
        UnfavorableBase = 2600m,
        Disinfectants = 0.10m,
        NightShifts = 0.40m,
    };

    [Fact]
    public void Teacher_FullMonth_ComputesOklad()
    {
        var result = new PayrollCalculator().Calculate(Input(22, 22, Teacher(oklad: 8397m, hours: 9m)));

        var oklad = result.Earnings.Single(e => e.Name == "Оклад");
        oklad.Amount.Should().Be(4198.5m);
        oklad.Formula.Should().Be("=8397/18*9");
    }

    [Fact]
    public void Teacher_PartialMonth_ProratesOklad()
    {
        var result = new PayrollCalculator().Calculate(Input(22, 11, Teacher(oklad: 8397m, hours: 9m)));

        var oklad = result.Earnings.Single(e => e.Name == "Оклад");
        oklad.Amount.Should().Be(2099.25m);              // 4198.5 × 11/22
        oklad.Formula.Should().Be("=8397/18*9/22*11");
    }

    [Fact]
    public void Teacher_Has1749_FortyPercentOfOklad()
    {
        var result = new PayrollCalculator().Calculate(Input(22, 22, Teacher(oklad: 8397m, hours: 9m)));

        var b1749 = result.Earnings.Single(e => e.Name == "Надбавка №1749");
        b1749.Amount.Should().Be(1679.4m);               // 4198.5 × 40%
        b1749.Formula.Should().Be("=4198.5*40%");
    }

    [Fact]
    public void Mop_HasNo1749()
    {
        var result = new PayrollCalculator().Calculate(Input(22, 22, Mop(oklad: 8000m)));

        result.Earnings.Should().NotContain(e => e.Name == "Надбавка №1749");
    }

    [Fact]
    public void Teacher_WithTitle_AddsTitleAllowance()
    {
        var result = new PayrollCalculator().Calculate(Input(22, 22, Teacher(oklad: 8397m, hours: 9m, titlePct: 0.10m)));

        var title = result.Earnings.Single(e => e.Name == "За звання");
        title.Amount.Should().Be(419.85m);               // 4198.5 × 10%
        title.Formula.Should().Be("=4198.5*10%");
    }

    [Fact]
    public void Teacher_NoTitle_NoTitleAllowance()
    {
        var result = new PayrollCalculator().Calculate(Input(22, 22, Teacher(oklad: 8397m, hours: 9m)));

        result.Earnings.Should().NotContain(e => e.Name == "За звання");
    }

    [Fact]
    public void Taxes_ComputedFromGross()
    {
        var result = new PayrollCalculator().Calculate(Input(22, 22, Mop(oklad: 8000m)));

        result.Gross.Should().Be(8000m);
        result.Deductions.Should().SatisfyRespectively(
            pdfo => { pdfo.Name.Should().Be("ПДФО"); pdfo.Amount.Should().Be(1440m); },
            vz => { vz.Name.Should().Be("Військовий збір"); vz.Amount.Should().Be(400m); },
            union => { union.Name.Should().Be("Профспілковий внесок"); union.Amount.Should().Be(80m); });
        result.TotalWithheld.Should().Be(1920m);
        result.NetPay.Should().Be(6080m);                // 8000 − 1920
    }

    [Fact]
    public void Manual_BonusInEarnings_AdvanceWithheld()
    {
        var input = Input(
            normDays: 22,
            workedDays: 22,
            positions: [Mop(oklad: 8000m)],
            manual: new ManualAdjustments { Bonus = 5000m, Advance = 8000m });

        var result = new PayrollCalculator().Calculate(input);

        result.Earnings.Should().Contain(e => e.Name == "Премія" && e.Amount == 5000m);
        result.Gross.Should().Be(13000m);                // 8000 + 5000
        result.Deductions.Should().Contain(d => d.Name == "Аванс" && d.Amount == 8000m);
    }

    // --- helpers: збирають мінімальний вхід, щоб тіло тесту лишалось коротким ---

    private static PositionCalcInput Teacher(decimal oklad, decimal hours, decimal titlePct = 0m) => new()
    {
        WorkerClass = WorkerClass.Pedagogical,
        PositionName = "Вчитель",
        Oklad = oklad,
        RateCount = 1m,
        PedHoursWeekly = hours,
        TitlePct = titlePct,
    };

    private static PositionCalcInput Mop(decimal oklad) => new()
    {
        WorkerClass = WorkerClass.MOP,
        PositionName = "Прибиральниця",
        Oklad = oklad,
        RateCount = 1m,
    };

    private static CalcInput Input(int normDays, decimal workedDays, params PositionCalcInput[] positions)
        => Input(normDays, workedDays, positions, new ManualAdjustments());

    private static CalcInput Input(int normDays, decimal workedDays, PositionCalcInput[] positions, ManualAdjustments manual) => new()
    {
        EmployeeId = 1,
        FullName = "Тест Тестович",
        Year = 2026,
        Month = 3,
        NormDays = normDays,
        WorkedDays = workedDays,
        Positions = positions,
        Manual = manual,
        Params = DefaultParams,
    };
}
