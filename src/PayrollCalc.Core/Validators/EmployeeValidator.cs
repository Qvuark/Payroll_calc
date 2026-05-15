namespace PayrollCalc.Core.Validators;
using PayrollCalc.Core.Entities.Enums;
public static class EmployeeValidator
{
    public static List<string>? ValidateBlocks(WorkerClass workerClass, bool hasWorkload, bool hasAdmin, bool hasAllowances, bool hasNonPedagogical)
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
                if (hasAllowances)
                    errors.Add("Спеціалісти не можуть мати доплати.");
                break;
            case WorkerClass.MOP:
                if (hasWorkload)
                    errors.Add("МНП не може мати навчальне навантаження.");
                if (hasAdmin)
                    errors.Add("МНП не може мати адміністративний блок.");
                if (hasAllowances)
                    errors.Add("МНП не може мати доплати.");
                break;
        }
        return errors.Count > 0 ? errors : null;
    }
}