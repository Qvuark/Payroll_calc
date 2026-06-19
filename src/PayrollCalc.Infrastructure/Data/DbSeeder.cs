using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;

namespace PayrollCalc.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedSystemParams(context);
        await SeedTariffGrades(context);
        await SeedWorkCalendar(context);
        await SeedTitleTypes(context);
        await SeedNotebookRates(context);
        await SeedDepartments(context);
        await SeedPositions(context);
    }

    private static async Task SeedSystemParams(AppDbContext context)
    {
        if (context.SystemParams.Any()) return;

        var date = new DateOnly(2026, 1, 1);
        context.SystemParams.AddRange(
            new SystemParam { Key = "pdfo_rate", Value = 0.18m, EffectiveDate = date },
            new SystemParam { Key = "vz_rate", Value = 0.05m, EffectiveDate = date },
            new SystemParam { Key = "esv_rate", Value = 0.22m, EffectiveDate = date },
            new SystemParam { Key = "union_rate", Value = 0.01m, EffectiveDate = date },
            new SystemParam { Key = "bonus_1749", Value = 0.40m, EffectiveDate = date },
            new SystemParam { Key = "prestige_rate", Value = 0.20m, EffectiveDate = date },
            new SystemParam { Key = "prestige_rate_director", Value = 0.25m, EffectiveDate = date },
            new SystemParam { Key = "mzp", Value = 8647m, EffectiveDate = date },
            new SystemParam { Key = "unfavorable_base", Value = 2600m, EffectiveDate = date },
            new SystemParam { Key = "cabinet_standard", Value = 0.13m, EffectiveDate = date },
            new SystemParam { Key = "cabinet_music_it", Value = 0.10m, EffectiveDate = date },
            new SystemParam { Key = "workshop", Value = 0.20m, EffectiveDate = date },
            new SystemParam { Key = "gym", Value = 0.10m, EffectiveDate = date },
            new SystemParam { Key = "shooting_range", Value = 0.20m, EffectiveDate = date },
            new SystemParam { Key = "social_benefit_living_min", Value = 3328m, EffectiveDate = date },
            new SystemParam { Key = "social_benefit_cap", Value = 4660m, EffectiveDate = date },
            new SystemParam { Key = "computers", Value = 0.10m, EffectiveDate = date },
            new SystemParam { Key = "extracurricular", Value = 0.10m, EffectiveDate = date },
            new SystemParam { Key = "website", Value = 0.10m, EffectiveDate = date },
            new SystemParam { Key = "inclusive_rate", Value = 0.20m, EffectiveDate = date },
            new SystemParam { Key = "class_mgmt_1_4", Value = 0.20m, EffectiveDate = date },
            new SystemParam { Key = "class_mgmt_5_11", Value = 0.25m, EffectiveDate = date },
            new SystemParam { Key = "military_accounting", Value = 0.05m, EffectiveDate = date },
            new SystemParam { Key = "notebook_foreign_lang", Value = 0.10m, EffectiveDate = date },
            new SystemParam { Key = "notebook_default", Value = 0.15m, EffectiveDate = date },
            new SystemParam { Key = "notebook_lang_lit", Value = 0.20m, EffectiveDate = date },
            // Надбавки МОП: дезінфектанти 10% (тільки уборщиці по наказу), нічні зміни 40% (за колдоговором).
            new SystemParam { Key = "disinfectants_rate", Value = 0.10m, EffectiveDate = date },
            new SystemParam { Key = "night_shifts_rate", Value = 0.40m, EffectiveDate = date }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedTariffGrades(AppDbContext context)
    {
        if (context.TariffGrades.Any()) return;

        var date = new DateOnly(2026, 1, 1);
        context.TariffGrades.AddRange(
            new TariffGrade { Grade = 1, MonthlyRate = 3470.00m, EffectiveDate = date },
            new TariffGrade { Grade = 2, MonthlyRate = 3782.00m, EffectiveDate = date },
            new TariffGrade { Grade = 3, MonthlyRate = 4095.00m, EffectiveDate = date },
            new TariffGrade { Grade = 4, MonthlyRate = 4407.00m, EffectiveDate = date },
            new TariffGrade { Grade = 5, MonthlyRate = 4719.00m, EffectiveDate = date },
            new TariffGrade { Grade = 6, MonthlyRate = 5032.00m, EffectiveDate = date },
            new TariffGrade { Grade = 7, MonthlyRate = 5344.00m, EffectiveDate = date },
            new TariffGrade { Grade = 8, MonthlyRate = 5691.00m, EffectiveDate = date },
            new TariffGrade { Grade = 9, MonthlyRate = 6003.00m, EffectiveDate = date },
            new TariffGrade { Grade = 10, MonthlyRate = 6315.00m, EffectiveDate = date },
            new TariffGrade { Grade = 11, MonthlyRate = 6836.00m, EffectiveDate = date },
            new TariffGrade { Grade = 12, MonthlyRate = 7356.00m, EffectiveDate = date },
            new TariffGrade { Grade = 13, MonthlyRate = 7877.00m, EffectiveDate = date },
            new TariffGrade { Grade = 14, MonthlyRate = 8397.00m, EffectiveDate = date },
            new TariffGrade { Grade = 15, MonthlyRate = 8953.00m, EffectiveDate = date },
            new TariffGrade { Grade = 16, MonthlyRate = 9681.00m, EffectiveDate = date },
            new TariffGrade { Grade = 17, MonthlyRate = 10410.00m, EffectiveDate = date },
            new TariffGrade { Grade = 18, MonthlyRate = 11139.00m, EffectiveDate = date },
            new TariffGrade { Grade = 19, MonthlyRate = 11867.00m, EffectiveDate = date },
            new TariffGrade { Grade = 20, MonthlyRate = 12631.00m, EffectiveDate = date },
            new TariffGrade { Grade = 21, MonthlyRate = 13360.00m, EffectiveDate = date },
            new TariffGrade { Grade = 22, MonthlyRate = 14088.00m, EffectiveDate = date },
            new TariffGrade { Grade = 23, MonthlyRate = 14817.00m, EffectiveDate = date },
            new TariffGrade { Grade = 24, MonthlyRate = 15129.00m, EffectiveDate = date },
            new TariffGrade { Grade = 25, MonthlyRate = 15650.00m, EffectiveDate = date }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedWorkCalendar(AppDbContext context)
    {
        if (context.WorkCalendars.Any()) return;

        // Робочі дні 2026, укр. виробничий календар. Воєнний стан: скасовано майже всі свята,
        // лишились 3 (24.08 пн, 01.10 чт, 25.12 пт) — їх віднято. Решта (1.01/8.03/Великдень/
        // 1.05/8.05/Трійця/28.06/15.07) тепер робочі. Звірити з бухгалтером Серпень/Жовтень/Грудень.
        var days = new[] { 22, 20, 22, 22, 21, 22, 23, 20, 22, 21, 21, 22 };
        for (var month = 1; month <= 12; month++)
            context.WorkCalendars.Add(new WorkCalendar { Year = 2026, Month = month, WorkDays = days[month - 1] });

        await context.SaveChangesAsync();
    }

    private static async Task SeedTitleTypes(AppDbContext context)
    {
        if (context.TitleTypes.Any())
            return;

        context.TitleTypes.AddRange(
          new TitleType
          {
              Name = "Старший вчитель",
              WorkerClass = WorkerClass.Pedagogical,
              Pct = 0.10m,
              ExcelAliases = ["ст.вчитель", "ст. вчитель", "ст вчитель", "старший вч.", "ст. вч."]
          },
          new TitleType
          {
              Name = "Вчитель-методист",
              WorkerClass = WorkerClass.Pedagogical,
              Pct = 0.15m,
              ExcelAliases = ["методист", "вч.методист", "вч. методист", "вчитель-методист"]
          },
          new TitleType
          {
              Name = "Психолог-методист",
              WorkerClass = WorkerClass.AdminPedagogical,
              Pct = 0.10m,
              ExcelAliases = ["психолог-методист", "псих.методист", "псих-методист"]
          },
          new TitleType
          {
              Name = "Педагог-організатор",
              WorkerClass = WorkerClass.AdminPedagogical,
              Pct = 0.10m,
              ExcelAliases = ["педагог-організатор", "пед.організатор", "організатор", "пед-організатор"]
          },
          new TitleType
          {
              Name = "Соціальний педагог",
              WorkerClass = WorkerClass.AdminPedagogical,
              Pct = 0.10m,
              ExcelAliases = ["соц.педагог", "соцпедагог", "соц-педагог", "соц. педагог"]
          }
          );
        await context.SaveChangesAsync();
    }

    private static async Task SeedNotebookRates(AppDbContext context)
    {
        if (context.NotebookRates.Any()) return;

        context.NotebookRates.AddRange(
            new NotebookRate { SubjectKeyword = "іноземна", Pct = 0.10m },
            new NotebookRate { SubjectKeyword = "математика", Pct = 0.15m },
            new NotebookRate { SubjectKeyword = "початкова", Pct = 0.15m },
            new NotebookRate { SubjectKeyword = "інформатика", Pct = 0.15m },
            new NotebookRate { SubjectKeyword = "укр", Pct = 0.20m },
            new NotebookRate { SubjectKeyword = "зарубіжна", Pct = 0.20m }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedDepartments(AppDbContext context)
    {
        if (context.Departments.Any()) return;

        context.Departments.AddRange(
            new Department { Name = "Адміністрація" },
            new Department { Name = "Педагогічний персонал" },
            new Department { Name = "Спеціалісти" },
            new Department { Name = "Господарська служба" }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedPositions(AppDbContext context)
    {
        if (context.Positions.Any()) return;

        // отримуємо Id департаментів — щоб не хардкодити числа які залежать від порядку вставки
        var admin = await context.Departments.SingleAsync(d => d.Name == "Адміністрація");
        var pedagogical = await context.Departments.SingleAsync(d => d.Name == "Педагогічний персонал");
        var specialists = await context.Departments.SingleAsync(d => d.Name == "Спеціалісти");
        var economic = await context.Departments.SingleAsync(d => d.Name == "Господарська служба");

        context.Positions.AddRange(
            // Class 1 — Педагогічні (вчителі, погодинна оплата)
            new Position
            {
                Name = "Вчитель",
                DepartmentId = pedagogical.Id,
                WorkerClass = WorkerClass.Pedagogical,
                ExcelAliases = new() { "вч.", "вчитель" }
            },
            // Class 2 — Адмін-педагогічні (фіксований оклад + можливе навантаження)
            new Position
            {
                Name = "Директор",
                DepartmentId = admin.Id,
                WorkerClass = WorkerClass.AdminPedagogical,
                ExcelAliases = new() { "директор" }
            },
            new Position
            {
                Name = "Заступник директора з НВР",
                DepartmentId = admin.Id,
                WorkerClass = WorkerClass.AdminPedagogical,
                ExcelAliases = new() { "заст.директора", "заст.дир.", "заст.директора з НВР" }
            },
            new Position
            {
                Name = "Практичний психолог",
                DepartmentId = pedagogical.Id,
                WorkerClass = WorkerClass.AdminPedagogical,
                ExcelAliases = new() { "практ.психолог", "пс.методист", "психолог" }
            },
            new Position
            {
                Name = "Педагог-організатор",
                DepartmentId = pedagogical.Id,
                WorkerClass = WorkerClass.AdminPedagogical,
                ExcelAliases = new() { "пед.організатор", "пед-організатор", "організатор" }
            },
            new Position
            {
                Name = "Соціальний педагог",
                DepartmentId = pedagogical.Id,
                WorkerClass = WorkerClass.AdminPedagogical,
                ExcelAliases = new() { "соц.педагог", "соцпедагог" }
            },
            new Position
            {
                Name = "Керівник гуртка",
                DepartmentId = pedagogical.Id,
                WorkerClass = WorkerClass.AdminPedagogical,
                ExcelAliases = new() { "кер.гуртка", "керівник гуртка" }
            },
            new Position
            {
                Name = "Асистент вчителя",
                DepartmentId = pedagogical.Id,
                WorkerClass = WorkerClass.AdminPedagogical,
                ExcelAliases = new() { "асистент вчителя", "ас.вчителя" }
            },
            new Position
            {
                Name = "Вихователь",
                DepartmentId = pedagogical.Id,
                WorkerClass = WorkerClass.AdminPedagogical,
                ExcelAliases = new() { "вихователь" }
            },

            // Class 3 — Спеціалісти (фіксований оклад, без підвищення №1749)
            new Position
            {
                Name = "Головний бухгалтер",
                DepartmentId = specialists.Id,
                WorkerClass = WorkerClass.Specialist,
                ExcelAliases = new() { "гол.бухгалтер", "гол. бухг." }
            },
            new Position
            {
                Name = "Бухгалтер",
                DepartmentId = specialists.Id,
                WorkerClass = WorkerClass.Specialist,
                ExcelAliases = new() { "бухгалтер" }
            },
            new Position
            {
                Name = "Завідувач бібліотеки",
                DepartmentId = specialists.Id,
                WorkerClass = WorkerClass.Specialist,
                ExcelAliases = new() { "зав.бібліотеки", "завбібліотеки", "бібліотекар" }
            },
            new Position
            {
                Name = "Заступник директора з господарської роботи",
                DepartmentId = admin.Id,
                WorkerClass = WorkerClass.Specialist,
                ExcelAliases = new() { "заст.дир. з ГР", "завгосп" }
            },
            new Position
            {
                Name = "Сестра медична",
                DepartmentId = specialists.Id,
                WorkerClass = WorkerClass.Specialist,
                ExcelAliases = new() { "сестра медична", "медсестра" }
            },
            new Position
            {
                Name = "Секретар-друкарка",
                DepartmentId = specialists.Id,
                WorkerClass = WorkerClass.Specialist,
                ExcelAliases = new() { "секретар", "секретар-друкарка" }
            },
            new Position
            {
                Name = "Лаборант",
                DepartmentId = specialists.Id,
                WorkerClass = WorkerClass.Specialist,
                ExcelAliases = new() { "лаборант" }
            },

            // Class 4 — МОП (молодший обслуговуючий персонал)
            new Position
            {
                Name = "Робітник з обслуговування будівель",
                DepartmentId = economic.Id,
                WorkerClass = WorkerClass.MOP,
                ExcelAliases = new() { "робітник", "робітник з ремонту", "робітник з обсл." }
            },
            new Position
            {
                Name = "Прибиральник службових приміщень",
                DepartmentId = economic.Id,
                WorkerClass = WorkerClass.MOP,
                ExcelAliases = new() { "прибиральник", "приб.службових" }
            },
            new Position
            {
                Name = "Сторож",
                DepartmentId = economic.Id,
                WorkerClass = WorkerClass.MOP,
                // Єдина погодинна посада: оклад = тариф/176×години з табеля, не за дні.
                IsHourly = true,
                ExcelAliases = new() { "сторож" }
            },
            new Position
            {
                Name = "Гардеробник",
                DepartmentId = economic.Id,
                WorkerClass = WorkerClass.MOP,
                ExcelAliases = new() { "гардеробник" }
            },
            new Position
            {
                Name = "Двірник",
                DepartmentId = economic.Id,
                WorkerClass = WorkerClass.MOP,
                ExcelAliases = new() { "двірник" }
            }
        );
        await context.SaveChangesAsync();
    }
}
