using System.ComponentModel.DataAnnotations;
using PayrollCalc.Core.Entities;

namespace PayrollCalc.Core.DTOs.Employee.Requests;

/// <summary>
/// Блок навантаження вчителя (Class 1). Години на тиждень за віковими групами,
/// індивідуальне навчання, перевірка зошитів та інклюзивні години.
/// </summary>
public class EmployeeWorkloadRequest
{
    [Range(0, 60)] public decimal Hours1To4 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal IndividualHours1To4 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal Hours5To9 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal IndividualHours5To9 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal Hours10To11 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal IndividualHours10To11 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal NotebookHours1To4 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal NotebookHours5To9 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal NotebookHours10To11 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal InclusiveHours1To4 { get; set; } = decimal.Zero;
    [Range(0, 60)] public decimal InclusiveHours5To9 { get; set; } = decimal.Zero;
    /// <summary>
    /// FK на NotebookRate — тариф за перевірку зошитів (різний для предметів).
    /// Null якщо вчитель зошитів не перевіряє.
    /// </summary>
    public int? NotebookRateId { get; set; }

    /// <summary>
    /// Маппінг Request → entity. EmployeePositionId виставляє контролер.
    /// </summary>
    /// <param name="request">Дані запиту.</param>
    /// <returns>Новий EmployeeWorkload, готовий до Add у DbContext.</returns>
    public static EmployeeWorkload FromRequest(EmployeeWorkloadRequest request)
    {
        return new EmployeeWorkload
        {
            Hours1To4 = request.Hours1To4,
            IndividualHours1To4 = request.IndividualHours1To4,
            Hours5To9 = request.Hours5To9,
            IndividualHours5To9 = request.IndividualHours5To9,
            Hours10To11 = request.Hours10To11,
            IndividualHours10To11 = request.IndividualHours10To11,
            NotebookHours1To4 = request.NotebookHours1To4,
            NotebookHours5To9 = request.NotebookHours5To9,
            NotebookHours10To11 = request.NotebookHours10To11,
            InclusiveHours1To4 = request.InclusiveHours1To4,
            InclusiveHours5To9 = request.InclusiveHours5To9,
            NotebookRateId = request.NotebookRateId
        };
    }
}
