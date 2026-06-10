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
                // Admin блок тепер містить лише педагогічні обов'язки (ClassMgmt, Cabinet, Gym, Shooting, Computers,
                // Extracurricular, Website) — DirectorPct/AdminRateCount/PedRateCount були дропнуті у Phase 3.6.5.
                // Тому Pedagogical (вчителі) можуть мати Admin блок: класне керівництво — норма для Class 1.
                if (hasNonPedagogical)
                    errors.Add("Педагогічний персонал не може мати непедагогічний блок.");
                break;
            case WorkerClass.AdminPedagogical:
                if (hasAdmin)
                    errors.Add("Адміністративно-педагогічний персонал не може мати адміністративний блок (класне керівництво/кабінет — лише вчителі).");
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
    /// <summary>
    /// Перевіряє чи дозволений тарифний розряд для WorkerClass посади.
    /// Діапазони per-class (драфт); точні per-position межі додаються у UI-валідації пізніше.
    /// </summary>
    /// <param name="workerClass">Клас посади.</param>
    /// <param name="grade">Номер тарифного розряду (TariffGrade.Grade).</param>
    /// <returns>true якщо розряд у дозволеному діапазоні класу.</returns>
    public static bool ValidateGradeForClass(WorkerClass workerClass, int grade)
    {
        switch (workerClass)
        {
            case WorkerClass.Pedagogical:
                return grade >= 10 && grade <= 15;
            case WorkerClass.AdminPedagogical:
                return grade >= 8 && grade <= 18;
            case WorkerClass.Specialist:
                return grade >= 4 && grade <= 13;
            case WorkerClass.MOP:
                return grade >= 1 && grade <= 8;
            default:
                return false;
        }
    }
    /// <summary>
    /// Дозволений розряд для блоку ГПД (вихователь групи продовженого дня): 10–14.
    /// </summary>
    public static bool ValidateGpdGrade(int grade) => grade is >= 10 and <= 14;
    /// <summary>
    /// Дозволений розряд для блоку ПКР (керівник гуртка): 10–12.
    /// </summary>
    public static bool ValidatePkrGrade(int grade) => grade is >= 10 and <= 12;
}
