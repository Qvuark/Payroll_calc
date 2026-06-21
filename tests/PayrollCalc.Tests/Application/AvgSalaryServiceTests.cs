using FluentAssertions;
using PayrollCalc.API.Application.AvgSalary;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Tests.Application;

/// <summary>
/// Юніт-тести проводки подій середньоденної: сервіс читає поля-входи події, дергає
/// калькулятор і розкладає результат у поля-виходи. Перевіряємо маппінг, виведення
/// відсотка зі стажу, ручний override та неоплачувані відпустки.
/// </summary>
public class AvgSalaryServiceTests
{
    private readonly AvgSalaryService _service = new();

    [Fact]
    public void ApplySick_MapsCalculatorResultIntoEntity()
    {
        // Еталон мами: 123236.58 / (365 − 11) = 348.13 середньоденна, стаж 8+ → 100%.
        var e = new SickLeave
        {
            BaseAmount = 123236.58m,
            BaseExcludedDays = 11,
            DaysTotal = 10,
            InsuranceSeniorityYrs = 8
        };

        _service.ApplySick(e);

        Math.Round(e.AverageDaily, 2, MidpointRounding.AwayFromZero).Should().Be(348.13m);
        e.PaymentPct.Should().Be(1.00m);
        e.BaseDays.Should().Be(354);
        e.DaysEmployer.Should().Be(5);
        e.DaysFss.Should().Be(5);
        e.TotalAmount.Should().Be(e.AmountEmployer + e.AmountFss);
    }

    [Fact]
    public void ApplySick_DerivesPaymentPctFromSeniority_WhenNotSet()
    {
        var e = new SickLeave { BaseAmount = 100000m, BaseExcludedDays = 0, DaysTotal = 3, InsuranceSeniorityYrs = 4 };

        _service.ApplySick(e);

        // Стаж 4 роки → 60%.
        e.PaymentPct.Should().Be(0.60m);
    }

    [Fact]
    public void ApplySick_RespectsManualPaymentPctOverride()
    {
        // Стаж 20 років дав би 100%, але вписаний ручний 50% має перемогти.
        var e = new SickLeave { BaseAmount = 100000m, BaseExcludedDays = 0, DaysTotal = 3, InsuranceSeniorityYrs = 20, PaymentPct = 0.50m };

        _service.ApplySick(e);

        e.PaymentPct.Should().Be(0.50m);
    }

    [Fact]
    public void ApplyVacation_MapsAnnualEtalon()
    {
        // Еталон директора: 272077.34 / 365 = 745.42; × 56 кал.днів = 41743.37.
        var e = new Vacation
        {
            VacationType = VacationType.Annual,
            BaseAmount = 272077.34m,
            BaseDays = 365,
            CalendarDays = 56
        };

        _service.ApplyVacation(e);

        Math.Round(e.AverageDaily!.Value, 2, MidpointRounding.AwayFromZero).Should().Be(745.42m);
        Math.Round(e.TotalAmount!.Value, 2, MidpointRounding.AwayFromZero).Should().Be(41743.37m);
    }

    [Fact]
    public void ApplyVacation_Compensation_UsesSameFormula()
    {
        var e = new Vacation
        {
            VacationType = VacationType.Compensation,
            BaseAmount = 100000m,
            BaseDays = 365,
            CalendarDays = 10
        };

        _service.ApplyVacation(e);

        e.TotalAmount.Should().NotBeNull();
        e.TotalAmount!.Value.Should().Be(e.AverageDaily!.Value * 10);
    }

    [Fact]
    public void ApplyVacation_Unpaid_LeavesOutputsNull()
    {
        var e = new Vacation
        {
            VacationType = VacationType.Unpaid,
            BaseAmount = 100000m,
            BaseDays = 365,
            CalendarDays = 14
        };

        _service.ApplyVacation(e);

        e.AverageDaily.Should().BeNull();
        e.TotalAmount.Should().BeNull();
    }

    [Fact]
    public void ApplyTraining_MapsCalculatorResultIntoEntity()
    {
        var e = new TrainingLeave { BaseAmount = 70000m, BaseWorkingDays = 42, WorkingDaysAbsent = 10 };

        _service.ApplyTraining(e);

        Math.Round(e.AverageDaily, 2, MidpointRounding.AwayFromZero).Should().Be(1666.67m);
        e.TotalAmount.Should().Be(e.AverageDaily * 10);
    }
}
