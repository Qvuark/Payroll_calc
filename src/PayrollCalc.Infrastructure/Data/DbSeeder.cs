using PayrollCalc.Core.Entities;

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
    }

    private static async Task SeedSystemParams(AppDbContext context)
    {
        if (context.SystemParams.Any()) return;

        var date = new DateOnly(2026, 1, 1);
        context.SystemParams.AddRange(
            new SystemParam { Key = "pdfo_rate",              Value = 0.18m,  EffectiveDate = date },
            new SystemParam { Key = "vz_rate",                Value = 0.05m,  EffectiveDate = date },
            new SystemParam { Key = "esv_rate",               Value = 0.22m,  EffectiveDate = date },
            new SystemParam { Key = "union_rate",             Value = 0.01m,  EffectiveDate = date },
            new SystemParam { Key = "bonus_1749",             Value = 0.40m,  EffectiveDate = date },
            new SystemParam { Key = "prestige_rate",          Value = 0.20m,  EffectiveDate = date },
            new SystemParam { Key = "prestige_rate_director", Value = 0.25m,  EffectiveDate = date },
            new SystemParam { Key = "mzp",                    Value = 8647m,  EffectiveDate = date },
            new SystemParam { Key = "unfavorable_base",       Value = 2600m,  EffectiveDate = date },
            new SystemParam { Key = "cabinet_standard",       Value = 0.13m,  EffectiveDate = date },
            new SystemParam { Key = "cabinet_music_it",       Value = 0.10m,  EffectiveDate = date },
            new SystemParam { Key = "workshop",               Value = 0.20m,  EffectiveDate = date },
            new SystemParam { Key = "gym",                    Value = 0.10m,  EffectiveDate = date },
            new SystemParam { Key = "shooting_range",         Value = 0.20m,  EffectiveDate = date },
            new SystemParam { Key = "computers",              Value = 0.10m,  EffectiveDate = date },
            new SystemParam { Key = "extracurricular",        Value = 0.10m,  EffectiveDate = date },
            new SystemParam { Key = "website",                Value = 0.10m,  EffectiveDate = date },
            new SystemParam { Key = "inclusive_rate",         Value = 0.20m,  EffectiveDate = date },
            new SystemParam { Key = "class_mgmt_1_4",         Value = 0.20m,  EffectiveDate = date },
            new SystemParam { Key = "class_mgmt_5_11",        Value = 0.25m,  EffectiveDate = date },
            new SystemParam { Key = "military_accounting",    Value = 0.05m,  EffectiveDate = date },
            new SystemParam { Key = "notebook_foreign_lang",  Value = 0.10m,  EffectiveDate = date },
            new SystemParam { Key = "notebook_default",       Value = 0.15m,  EffectiveDate = date },
            new SystemParam { Key = "notebook_lang_lit",      Value = 0.20m,  EffectiveDate = date }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedTariffGrades(AppDbContext context)
    {
        if (context.TariffGrades.Any()) return;

        var date = new DateOnly(2026, 1, 1);
        context.TariffGrades.AddRange(
            new TariffGrade { Grade = 14, MonthlyRate = 8397.00m,  EffectiveDate = date },
            new TariffGrade { Grade = 16, MonthlyRate = 9889.50m,  EffectiveDate = date },
            new TariffGrade { Grade = 17, MonthlyRate = 10410.00m, EffectiveDate = date },
            new TariffGrade { Grade = 99, MonthlyRate = 6773.00m,  EffectiveDate = date },
            new TariffGrade { Grade = 98, MonthlyRate = 6836.00m,  EffectiveDate = date }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedWorkCalendar(AppDbContext context)
    {
        if (context.WorkCalendars.Any()) return;

        var days = new[] { 19, 20, 21, 21, 19, 20, 23, 20, 22, 21, 21, 22 };
        for (var month = 1; month <= 12; month++)
            context.WorkCalendars.Add(new WorkCalendar { Year = 2026, Month = month, WorkDays = days[month - 1] });

        await context.SaveChangesAsync();
    }

    private static async Task SeedTitleTypes(AppDbContext context)
    {
        if (context.TitleTypes.Any()) return;

        context.TitleTypes.AddRange(
            new TitleType { Name = "Старший вчитель",  Pct = 0.10m },
            new TitleType { Name = "Вчитель-методист", Pct = 0.15m }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedNotebookRates(AppDbContext context)
    {
        if (context.NotebookRates.Any()) return;

        context.NotebookRates.AddRange(
            new NotebookRate { SubjectKeyword = "іноземна",   Pct = 0.10m },
            new NotebookRate { SubjectKeyword = "математика",  Pct = 0.15m },
            new NotebookRate { SubjectKeyword = "початкова",   Pct = 0.15m },
            new NotebookRate { SubjectKeyword = "інформатика", Pct = 0.15m },
            new NotebookRate { SubjectKeyword = "укр",         Pct = 0.20m },
            new NotebookRate { SubjectKeyword = "зарубіжна",   Pct = 0.20m }
        );
        await context.SaveChangesAsync();
    }
}
