namespace PayrollCalc.Core.Entities.Enums;

/// <summary>
/// Категорія працівника, фундаментальна для розрахунку зарплати.
/// Прив'язана до Position (не до Employee — один працівник може мати ставки різних класів).
/// Визначає набір дозволених блоків надбавок та формули нарахування.
/// </summary>
public enum WorkerClass
{
    /// <summary>Вчителі. Блоки: Workload, Gpd, Pkr. Bonus #1749 = 40%, вислуга, престижність.</summary>
    Pedagogical = 1,
    /// <summary>Адмін-педагогічний (директор, заступник, психолог). Блоки: Workload, Admin, Gpd, Pkr.</summary>
    AdminPedagogical = 2,
    /// <summary>Спеціалісти (бухгалтер, бібліотекар, соц.педагог). Блоки: NonPedagogical. Без bonus #1749.</summary>
    Specialist = 3,
    /// <summary>МОП (молодший обслуговуючий персонал — двірник, прибиральник, сторож). Блоки: NonPedagogical. Без bonus #1749 і без вислуги.</summary>
    MOP = 4
}


