namespace PayrollCalc.Core.Entities.Enums;

/// <summary>
/// Статус працівника. Server-controlled (не передається з клієнта при Create —
/// новий завжди Active, перехід через PUT).
/// </summary>
public enum EmployeeStatus
{
    /// <summary>Активний — зарплата нараховується.</summary>
    Active,
    /// <summary>У відпустці (декрет, навчальна) — зарплата нараховується частково.</summary>
    OnLeave,
    /// <summary>Звільнений — soft delete, DismissalDate обов'язкова.</summary>
    Dismissed
}
