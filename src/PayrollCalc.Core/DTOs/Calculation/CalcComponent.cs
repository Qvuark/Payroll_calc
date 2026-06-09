using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.DTOs.Calculation;

/// <summary>
/// Один рядок розрахунку (одна надбавка чи утримання). Несе три речі одразу:
/// людську назву (як у розрахунковому листі), точну суму (decimal, БЕЗ проміжного округлення)
/// та Excel-формулу з підставленими числами — її пишемо прямо в клітинку, щоб бухгалтер
/// клікнув і побачив звідки число (напр. Name="Оклад за 9 год", Amount=4198.50, Formula="=8397/18*9").
/// </summary>
/// <param name="Name">Назва рядка українською.</param>
/// <param name="Amount">Сума (грн), повна точність.</param>
/// <param name="Formula">Excel-формула з літеральними числами (з "=").</param>
public record CalcComponent(string Name, decimal Amount, string Formula)
{
    /// <summary>
    /// Клас ставки, що породила компонент — щоб відомість клала оклад/№1749/звання/вислугу
    /// у J-колонки (адмін/спец/МОП) чи N-колонки (педагог). null — рядок рівня працівника (МЗП, ручні).
    /// </summary>
    public WorkerClass? SourceClass { get; init; }
}
