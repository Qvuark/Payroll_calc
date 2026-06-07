using FluentAssertions;
using PayrollCalc.Calculation;
using PayrollCalc.Core.DTOs.Calculation;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Tests.Calculation;

/// <summary>
/// Юніт-тести рушія (без БД): оклад, пропорція за відпрацьовані дні, податки, мануальні суми.
/// Числа звірені вручну з еталоном (відомість).
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
        var input = Input(normDays: 22, workedDays: 22, Teacher(oklad: 8397m, hours: 9m));

        var result = new PayrollCalculator().Calculate(input);

        var oklad = result.Earnings.Should().ContainSingle().Which;
        oklad.Name.Should().Be("Оклад");
        oklad.Amount.Should().Be(4198.5m);
        oklad.Formula.Should().Be("=8397/18*9");
        result.Gross.Should().Be(4198.5m);
    }

    [Fact]
    public void Teacher_PartialMonth_ProratesByDays()
    {
        var input = Input(normDays: 22, workedDays: 11, Teacher(oklad: 8397m, hours: 9m));

        var oklad = new PayrollCalculator().Calculate(input).Earnings.Single();

        oklad.Amount.Should().Be(2099.25m);              // 4198.5 × 11/22
        oklad.Formula.Should().Be("=8397/18*9/22*11");
    }

    [Fact]
    public void Taxes_ComputedFromGross()
    {
        var input = Input(normDays: 22, workedDays: 22, Teacher(oklad: 8397m, hours: 9m));

        var result = new PayrollCalculator().Calculate(input);

        result.Deductions.Should().SatisfyRespectively(
            pdfo => { pdfo.Name.Should().Be("ПДФО"); pdfo.Amount.Should().Be(755.73m); },
            vz => { vz.Name.Should().Be("Військовий збір"); vz.Amount.Should().Be(209.925m); },
            union => { union.Name.Should().Be("Профспілковий внесок"); union.Amount.Should().Be(41.985m); });
        result.TotalWithheld.Should().Be(1007.64m);
        result.NetPay.Should().Be(3190.86m);             // 4198.5 − 1007.64
    }

    [Fact]
    public void Manual_BonusInEarnings_AdvanceWithheld()
    {
        var input = Input(
            normDays: 22,
            workedDays: 22,
            positions: [Teacher(oklad: 8397m, hours: 9m)],
            manual: new ManualAdjustments { Bonus = 5000m, Advance = 8000m });

        var result = new PayrollCalculator().Calculate(input);

        result.Earnings.Should().Contain(e => e.Name == "Премія" && e.Amount == 5000m);
        result.Gross.Should().Be(9198.5m);               // 4198.5 + 5000
        result.Deductions.Should().Contain(d => d.Name == "Аванс" && d.Amount == 8000m);
    }

    // --- helpers: збирають мінімальний вхід, щоб тіло тесту лишалось коротким ---

    private static PositionCalcInput Teacher(decimal oklad, decimal hours) => new()
    {
        WorkerClass = WorkerClass.Pedagogical,
        PositionName = "Вчитель",
        Oklad = oklad,
        RateCount = 1m,
        PedHoursWeekly = hours,
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
