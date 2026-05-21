using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Employees.Requests;

/// <summary>
/// Блок непедагогічних надбавок ставки. Включає фіксовані гривневі суми
/// (наставництво, бібліотека, підручники) та МОП-надбавки (нічні зміни, дезінфектанти).
/// На відміну від EmployeeAdmin, тут не відсотки — а конкретні суми по наказу директора.
/// </summary>
public class EmployeeNonPedagogicalRequest
{
    public bool HasDisinfectants { get; set; }
    public bool HasNightShifts { get; set; }
    public bool HasMentor { get; set; }
    /// <summary>
    /// Сума надбавки за наставництво молодим вчителям. Задається бухгалтером по наказу.
    /// </summary>
    [Range(0.0, 100000.0)] public decimal MentorAmount { get; set; }
    public bool HasLibraryMgmt { get; set; }
    /// <summary>
    /// Сума надбавки за завідування бібліотекою. Задається бухгалтером по наказу.
    /// </summary>
    [Range(0.0, 100000.0)] public decimal LibraryMgmtAmount { get; set; }
    public bool HasTextbooks { get; set; }
    /// <summary>
    /// Сума надбавки за облік підручників. Задається бухгалтером по наказу.
    /// </summary>
    [Range(0.0, 100000.0)] public decimal TextbooksAmount { get; set; }

    /// <summary>
    /// Маппінг Request → entity. EmployeePositionId виставляє контролер.
    /// </summary>
    /// <param name="request">Дані запиту.</param>
    /// <returns>Новий EmployeeNonPedagogical, готовий до Add у DbContext.</returns>
    public static EmployeeNonPedagogical FromRequest(EmployeeNonPedagogicalRequest request)
    {
        return new EmployeeNonPedagogical
        {
            HasDisinfectants = request.HasDisinfectants,
            HasNightShifts = request.HasNightShifts,
            HasMentor = request.HasMentor,
            MentorAmount = request.MentorAmount,
            HasLibraryMgmt = request.HasLibraryMgmt,
            LibraryMgmtAmount = request.LibraryMgmtAmount,
            HasTextbooks = request.HasTextbooks,
            TextbooksAmount = request.TextbooksAmount
        };
    }
}
