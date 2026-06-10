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
        // Оклад вище МЗП (8647) — щоб доплата до мінімалки не спрацювала і gross лишався чистим.
        var result = new PayrollCalculator().Calculate(Input(22, 22, Mop(oklad: 10000m)));

        result.Gross.Should().Be(10000m);
        result.Deductions.Should().SatisfyRespectively(
            pdfo => { pdfo.Name.Should().Be("ПДФО"); pdfo.Amount.Should().Be(1800m); },
            vz => { vz.Name.Should().Be("Військовий збір"); vz.Amount.Should().Be(500m); },
            union => { union.Name.Should().Be("Профспілковий внесок"); union.Amount.Should().Be(100m); });
        result.TotalWithheld.Should().Be(2400m);
        result.NetPay.Should().Be(7600m);                // 10000 − 2400
    }

    [Fact]
    public void Manual_BonusInEarnings_AdvanceWithheld()
    {
        var input = Input(
            normDays: 22,
            workedDays: 22,
            positions: [Mop(oklad: 10000m)],
            manual: new ManualAdjustments { Bonus = 5000m, Advance = 8000m });

        var result = new PayrollCalculator().Calculate(input);

        result.Earnings.Should().Contain(e => e.Name == "Премія" && e.Amount == 5000m);
        result.Gross.Should().Be(15000m);                // 10000 + 5000
        result.Deductions.Should().Contain(d => d.Name == "Аванс" && d.Amount == 8000m);
    }

    [Fact]
    public void Manual_AllAmounts_NamedComponents_SickFssReducesUnion()
    {
        // Усі 7 ручних сум разом. Імена компонентів = контракт із PersistAsync (він шукає їх по цих рядках).
        var input = Input(
            normDays: 22,
            workedDays: 22,
            positions: [Mop(oklad: 10000m)],
            manual: new ManualAdjustments
            {
                Bonus = 5000m, Vacation = 3000m, SickEmployer = 1000m, SickFss = 2000m,
                Recalculation = 500m, EnforcementOrders = 700m, Advance = 8000m,
            });

        var result = new PayrollCalculator().Calculate(input);

        result.Earnings.Should().Contain(e => e.Name == "Відпускні" && e.Amount == 3000m);
        result.Earnings.Should().Contain(e => e.Name == "Лікарняні (роботодавець)" && e.Amount == 1000m);
        result.Earnings.Should().Contain(e => e.Name == "Лікарняні (ФСС)" && e.Amount == 2000m);
        result.Earnings.Should().Contain(e => e.Name == "Перерахунок" && e.Amount == 500m);

        result.Gross.Should().Be(21500m);                // 10000 + 5000 + 3000 + 1000 + 2000 + 500

        // Профспілка = (gross − лікарняні ФСС) × 1% = (21500 − 2000) × 1%.
        result.Deductions.Single(d => d.Name == "Профспілковий внесок").Amount.Should().Be(195m);
    }

    [Fact]
    public void LowPaid_ToppedUpToMinimumWage()
    {
        // МОП з окладом 8000 < МЗП 8647 → доплата 647 тягне gross до мінімалки.
        var result = new PayrollCalculator().Calculate(Input(22, 22, Mop(oklad: 8000m)));

        var topUp = result.Earnings.Single(e => e.Name == "Доплата до МЗП");
        topUp.Amount.Should().Be(647m);                  // 8647 − 8000
        topUp.Formula.Should().Be("=8647-8000");
        result.Gross.Should().Be(8647m);
    }

    [Fact]
    public void AboveMinimum_NoTopUp()
    {
        var result = new PayrollCalculator().Calculate(Input(22, 22, Mop(oklad: 10000m)));

        result.Earnings.Should().NotContain(e => e.Name == "Доплата до МЗП");
    }

    [Fact]
    public void Teacher_BelowMinimum_NoTopUp_ClassNotEligible()
    {
        // Доплата до МЗП лише Class 3/4. Вчитель (Class 1) з мізерним навантаженням нижче мінімалки — без доплати.
        var result = new PayrollCalculator().Calculate(Input(22, 22, Teacher(oklad: 8397m, hours: 1m)));

        result.Earnings.Should().NotContain(e => e.Name == "Доплата до МЗП");
    }

    [Fact]
    public void Tenure_FromRaisedBase_OkladPlus1749PlusTitle()
    {
        // raisedBase = 4198.5 (оклад) + 1679.4 (1749) + 419.85 (звання 10%) = 6297.75
        var pos = Teacher(oklad: 8397m, hours: 9m, titlePct: 0.10m) with { TenurePct = 0.30m };
        var result = new PayrollCalculator().Calculate(Input(22, 22, pos));

        var tenure = result.Earnings.Single(e => e.Name == "Вислуга років");
        tenure.Amount.Should().Be(1889.325m);            // 6297.75 × 30%
        tenure.Formula.Should().Be("=6297.75*30%");
    }

    [Fact]
    public void Prestige_FromRaisedBase_SameBaseAsTenure()
    {
        var pos = Teacher(oklad: 8397m, hours: 9m, titlePct: 0.10m) with { PrestigePct = 0.20m };
        var result = new PayrollCalculator().Calculate(Input(22, 22, pos));

        var prestige = result.Earnings.Single(e => e.Name == "Престижність");
        prestige.Amount.Should().Be(1259.55m);           // 6297.75 × 20%
    }

    [Fact]
    public void ClassManagement_FromTariffPlus1749_NoTitle()
    {
        // База класного — тариф+1749 БЕЗ звання: 8397 × 1.4 = 11755.8
        var pos = Teacher(oklad: 8397m, hours: 9m, titlePct: 0.10m)
            with { ClassManagementGroup = ClassGradeGroup.Grades1To4 };
        var result = new PayrollCalculator().Calculate(Input(22, 22, pos));

        var cm = result.Earnings.Single(e => e.Name == "Класне керівництво");
        cm.Amount.Should().Be(2351.16m);                 // 11755.8 × 20%
        cm.Formula.Should().Be("=11755.8*20%");
    }

    [Fact]
    public void Librarian_TenureHeadTextbooks_FromOklad()
    {
        // Костенко r58 еталон: J=7356, V=J×30% (стаж 20+ → TenurePct 0.30), W=J×50%, X=J×8%
        var pos = Specialist(oklad: 7356m) with { HasLibrarianTenure = true, IsLibraryHead = true, HasTextbooks = true, TenurePct = 0.30m };
        var result = new PayrollCalculator().Calculate(Input(21, 21, pos));

        result.Earnings.Single(e => e.Name == "Вислуга бібліотекаря").Amount.Should().Be(2206.8m);
        result.Earnings.Single(e => e.Name == "Завідування бібліотекою").Amount.Should().Be(3678m);
        result.Earnings.Single(e => e.Name == "За підручники").Amount.Should().Be(588.48m);
    }

    [Fact]
    public void Medic_Tenure_ThirtyPercentOfOklad()
    {
        // Суховіцька r57 еталон: J=6003, Y=J×30% (стаж 20+ → TenurePct 0.30)
        var pos = Specialist(oklad: 6003m) with { HasMedicTenure = true, TenurePct = 0.30m };
        var result = new PayrollCalculator().Calculate(Input(23, 23, pos));

        result.Earnings.Single(e => e.Name == "Вислуга медпрацівника").Amount.Should().Be(1800.9m);
    }

    [Fact]
    public void Guard_NightShift_TariffOver176()
    {
        // Ковальов r75 еталон: =3782/176*122*40%
        var pos = Mop(oklad: 3782m) with { NightHours = 122m };
        var result = new PayrollCalculator().Calculate(Input(22, 22, pos));

        var night = result.Earnings.Single(e => e.Name == "Доплата за нічні");
        night.Amount.Should().BeApproximately(1048.65m, 0.01m);   // 3782/176×122×40%
        night.Formula.Should().Be("=3782/176*122*40%");
    }

    [Fact]
    public void Guard_Hourly_OkladAndMinimumByHours()
    {
        // Шамрай r76: погодинний сторож. J=3782/176×187, МЗП=8647/176×187, нічні=3782/176×126×40%.
        var pos = Mop(oklad: 3782m) with { IsHourly = true, WorkedHours = 187m, NightHours = 126m };
        var result = new PayrollCalculator().Calculate(Input(22, 22, pos));

        // decimal-хвіст від ділення (3782/176) — норма при «без проміжного округлення», ріжеться на виводі.
        var oklad = result.Earnings.Single(e => e.Name == "Оклад");
        oklad.Amount.Should().BeApproximately(4018.375m, 0.0001m);   // 3782/176×187
        oklad.Formula.Should().Be("=3782/176*187");

        var mzp = result.Earnings.Single(e => e.Name == "Доплата до МЗП");
        mzp.Amount.Should().BeApproximately(5169.0625m, 0.0001m);    // 8647/176×187 − оклад
        mzp.Formula.Should().Be("=8647/176*187-4018.375");

        result.Earnings.Single(e => e.Name == "Доплата за нічні").Amount
            .Should().BeApproximately(1083.03m, 0.01m);
    }

    [Fact]
    public void AdminInclusive_PartialMonth_ProratesOnlyOnce()
    {
        // Інклюзив адміна — від СИРОГО тарифу з одною пропорцією: 10410×140%×20% × 11/22 = 1457.4.
        // Від обчисленого окладу вийшло б ×(11/22)² — подвійне урізання.
        var admin = new PositionCalcInput
        {
            WorkerClass = WorkerClass.AdminPedagogical, PositionName = "Директор",
            Oklad = 10410m, RateCount = 1m, InclusiveHours = 1m,
        };
        var result = new PayrollCalculator().Calculate(Input(22, 11, admin));

        var inclusive = result.Earnings.Single(e => e.Name == "Інклюзивні класи");
        inclusive.Amount.Should().Be(1457.4m);
        inclusive.Formula.Should().Be("=14574*20%/22*11");
    }

    [Fact]
    public void Manual_HolidayAndAnnualBonus_AppearInEarnings()
    {
        var manual = new ManualAdjustments { Holiday = 300m, AnnualBonus = 1000m };
        var result = new PayrollCalculator().Calculate(Input(22, 22, [Mop(8000m)], manual));

        result.Earnings.Single(e => e.Name == "Святкові").Amount.Should().Be(300m);
        result.Earnings.Single(e => e.Name == "Щорічна винагорода").Amount.Should().Be(1000m);
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

    private static PositionCalcInput Specialist(decimal oklad) => new()
    {
        WorkerClass = WorkerClass.Specialist,
        PositionName = "Спеціаліст",
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
