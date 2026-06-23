namespace PayrollCalc.API.Application.AvgSalary;

/// <summary>
/// Вид події для збору авто-бази: визначає і вікно історії (12 міс / 2 міс),
/// і колонку довідника включення (яку галочку дивитись при підсумовуванні компонентів).
/// </summary>
public enum AvgBaseKind { Sick, Vacation, Training, Compensation }
