namespace PayrollCalc.Core.Entities.Enums;

/// <summary>
/// Вид спеціальної вислуги посади: бібліотекар і медсестра отримують вислугу за стажем
/// в окремих колонках відомості (V / Y), не в загальній M/Q. Решта посад — None.
/// </summary>
public enum SpecialTenureKind
{
    None = 0,
    Librarian = 1,
    Medic = 2,
}
