using PayrollCalc.Core.DTOs.Calculation;
using static PayrollCalc.Calculation.CalcFormat;

namespace PayrollCalc.Calculation.Calculators;

/// <summary>
/// Надбавка за вислугу років — відсоток від "окладу з підвищенням" (10/20/30% за стажем).
/// База — не голий оклад, а сума оклад + №1749 + звання (відомість: M=(J+K+L)×%, Q=(N+O+P)×%).
/// Відсоток береться зі ставки (PositionCalcInput.TenurePct); 0 → надбавки немає (МОП без стажу).
/// Бібліотекар/медсестра сюди не входять: їхня вислуга живе у власних колонках V/Y
/// (Librarian/MedicTenureCalculator) — інакше була б подвійна (в еталоні M у них порожня).
/// </summary>
public static class TenureCalculator
{
    /// <param name="raisedBase">Оклад з підвищенням: сума окладу, №1749 та звання цієї ставки.</param>
    public static CalcComponent? Calc(PositionCalcInput pos, decimal raisedBase)
    {
        if (pos.TenurePct == 0 || pos.HasLibrarianTenure || pos.HasMedicTenure)
            return null;

        var amount = raisedBase * pos.TenurePct;
        var formula = $"={Num(raisedBase)}*{Num(pos.TenurePct * 100)}%";
        return new CalcComponent("Вислуга років", amount, formula);
    }
}
