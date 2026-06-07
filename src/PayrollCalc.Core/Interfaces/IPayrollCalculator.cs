using PayrollCalc.Core.DTOs.Calculation;

namespace PayrollCalc.Core.Interfaces;

/// <summary>
/// Рушій розрахунку зарплати: чиста функція вхід→вихід, без БД та Excel.
/// Реалізація в проєкті Calculation; виклик — з Application (API).
/// </summary>
public interface IPayrollCalculator
{
    /// <summary>
    /// Рахує зарплату одного працівника за місяць.
    /// </summary>
    CalcResult Calculate(CalcInput input);
}
