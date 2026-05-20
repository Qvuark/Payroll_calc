using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Core.Validators;

/// <summary>
/// Валідатор бізнес-правил для блоків EmployeePosition. Перевіряє чи дозволено
/// певний блок (Workload/Admin/Gpd/Pkr/NonPedagogical) для даного WorkerClass посади.
/// </summary>
public static class EmployeeValidator
{
    /// <summary>
    /// Перевіряє чи відповідають передані блоки правилам WorkerClass посади.
    /// Викликається у контролері перед додаванням/оновленням ставки або її блоків.
    /// </summary>
    /// <param name="workerClass">Клас посади (Pedagogical / AdminPedagogical / Specialist / MOP).</param>
    /// <param name="hasWorkload">Чи переданий блок навантаження.</param>
    /// <param name="hasAdmin">Чи переданий адмін-блок.</param>
    /// <param name="hasNonPedagogical">Чи переданий непедагогічний блок.</param>
    /// <param name="hasGpd">Чи переданий блок ГПД.</param>
    /// <param name="hasPkr">Чи переданий блок ПКР.</param>
    /// <returns>Список помилок або null якщо помилок немає.</returns>
    public static List<string>? ValidateBlocks(
        WorkerClass workerClass,
        bool hasWorkload,
        bool hasAdmin,
        bool hasNonPedagogical,
        bool hasGpd,
        bool hasPkr)
    {
        var errors = new List<string>();
        switch (workerClass)
        {
            case WorkerClass.Pedagogical:
                if (hasAdmin)
                    errors.Add("Педагогічний персонал не може мати адміністративний блок.");
                if (hasNonPedagogical)
                    errors.Add("Педагогічний персонал не може мати непедагогічний блок.");
                break;
            case WorkerClass.AdminPedagogical:
                if (hasNonPedagogical)
                    errors.Add("Адміністративно-педагогічний персонал не може мати непедагогічний блок.");
                break;
            case WorkerClass.Specialist:
                if (hasWorkload)
                    errors.Add("Спеціалісти не можуть мати навчальне навантаження.");
                if (hasAdmin)
                    errors.Add("Спеціалісти не можуть мати адміністративний блок.");
                if (hasGpd)
                    errors.Add("Спеціалісти не можуть мати ГПД.");
                if (hasPkr)
                    errors.Add("Спеціалісти не можуть мати ПКР.");
                break;
            case WorkerClass.MOP:
                if (hasWorkload)
                    errors.Add("МОП не може мати навчальне навантаження.");
                if (hasAdmin)
                    errors.Add("МОП не може мати адміністративний блок.");
                if (hasGpd)
                    errors.Add("МОП не може мати ГПД.");
                if (hasPkr)
                    errors.Add("МОП не може мати ПКР.");
                break;
        }
        return errors.Count > 0 ? errors : null;
    }

}
