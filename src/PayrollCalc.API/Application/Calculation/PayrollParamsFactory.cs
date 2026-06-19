using PayrollCalc.Core.DTOs.Calculation;

namespace PayrollCalc.API.Application.Calculation;

/// <summary>
/// Збирає типізований PayrollParams з мапи SystemParams (Key→Value). Якщо ключа немає — кидає,
/// щоб розрахунок не побіг з мовчазним нулем у ставці податку (краще явна помилка конфігурації).
/// </summary>
public static class PayrollParamsFactory
{
    public static PayrollParams From(IReadOnlyDictionary<string, decimal> p) => new()
    {
        Pdfo = Get(p, "pdfo_rate"),
        Vz = Get(p, "vz_rate"),
        Union = Get(p, "union_rate"),
        Bonus1749 = Get(p, "bonus_1749"),
        Mzp = Get(p, "mzp"),
        UnfavorableBase = Get(p, "unfavorable_base"),
        Disinfectants = Get(p, "disinfectants_rate"),
        NightShifts = Get(p, "night_shifts_rate"),
        SocialBenefitLivingMin = Get(p, "social_benefit_living_min"),
        SocialBenefitCap = Get(p, "social_benefit_cap"),
    };

    private static decimal Get(IReadOnlyDictionary<string, decimal> p, string key)
        => p.TryGetValue(key, out var v)
            ? v
            : throw new InvalidOperationException($"Відсутній системний параметр '{key}' — налаштуйте SystemParams.");
}
