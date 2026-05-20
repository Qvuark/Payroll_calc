using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayrollCalc.Core.DTOs.Employee;
using PayrollCalc.Core.DTOs.Employee.Requests;
using PayrollCalc.Core.Entities;
using PayrollCalc.Core.Entities.Enums;
using PayrollCalc.Core.Validators;
using PayrollCalc.Infrastructure.Data;

namespace PayrollCalc.API.Controllers;

/// <summary>
/// Per-employee ставки (EmployeePosition) + блоки надбавок (Workload/Admin/Gpd/Pkr/NonPedagogical).
/// Route nested під працівника: /api/employees/{employeeId}/positions/...
/// </summary>
[ApiController]
[Route("api/employees/{employeeId}/positions")]
public class EmployeePositionsController(AppDbContext context) : ControllerBase
{
    /// <summary>
    /// Додає нову ставку працівнику. Якщо у нього 0 активних ставок або клієнт
    /// явно вказав IsPrimary=true — нова стає головною, прапор у старих знімається.
    /// </summary>
    /// <param name="employeeId">Id працівника з route.</param>
    /// <param name="request">Дані ставки (PositionId, TariffGradeId, RateCount, HireDate, прапори).</param>
    /// <returns>200 з EmployeePositionDto, 404 якщо працівник/посада/тариф не знайдені, 409 при дублі.</returns>
    [HttpPost]
    public async Task<ActionResult<EmployeePositionDto>> CreatePosition(
        int employeeId,
        CreateEmployeePositionRequest request)
    {
        var employee = await context.Employees
            .Include(employee => employee.Positions)
            .FirstOrDefaultAsync(employee => employee.Id == employeeId);
        if (employee == null)
            return NotFound($"Працівник з id={employeeId} не знайдений.");
        var position = await context.Positions.FindAsync(request.PositionId);
        if (position == null)
            return NotFound($"Посада з id={request.PositionId} не знайдена.");
        var tariffGrade = await context.TariffGrades.FindAsync(request.TariffGradeId);
        if (tariffGrade == null)
            return NotFound($"Тарифний розряд з id={request.TariffGradeId} не знайдений.");
        var duplicate = employee.Positions.Any(ep => ep.PositionId == position.Id && ep.DismissalDate == null);
        if (duplicate)
            return Conflict($"Працівник вже має активну ставку на посаді {position.Name}.");
        var activeCount = employee.Positions.Count(ep => ep.DismissalDate == null);
        var isPrimary = request.IsPrimary || activeCount == 0;
        if (isPrimary)
        {
            foreach (var p in employee.Positions.Where(p => p.IsPrimary && p.DismissalDate == null))
                p.IsPrimary = false;
        }
        var newPosition = CreateEmployeePositionRequest.FromRequest(request, employeeId);
        newPosition.IsPrimary = isPrimary;
        newPosition.EffectiveFrom = request.HireDate;
        context.EmployeePositions.Add(newPosition);
        await context.SaveChangesAsync();
        var saved = await LoadFullPositionAsync(newPosition.Id);
        if (saved == null)
            return BadRequest("Не вдалося зберегти.");
        return Ok(EmployeePositionDto.FromEntity(saved));
    }

    /// <summary>
    /// Оновлює існуючу ставку. PositionId і HireDate не змінюються — при зміні
    /// посади звільнюйте стару (DismissalDate) і створюйте нову через POST.
    /// </summary>
    /// <param name="employeeId">Id працівника з route.</param>
    /// <param name="posId">Id ставки з route.</param>
    /// <param name="request">Поля для оновлення (TariffGradeId, RateCount, IsPrimary, DismissalDate, прапори).</param>
    /// <returns>200 з EmployeePositionDto, 404 якщо не знайдено, 400 при невалідному стані.</returns>
    [HttpPut("{posId}")]
    public async Task<ActionResult<EmployeePositionDto>> UpdatePosition(
        int employeeId,
        int posId,
        UpdateEmployeePositionRequest request)
    {
        var employee = await context.Employees
            .Include(employee => employee.Positions)
            .FirstOrDefaultAsync(employee => employee.Id == employeeId);
        if (employee == null)
            return NotFound($"Працівник з id={employeeId} не знайдений.");
        var position = employee.Positions.FirstOrDefault(p => p.Id == posId);
        if (position == null)
            return NotFound($"Ставка з id={posId} не належить працівнику.");
        var tariffGrade = await context.TariffGrades.FindAsync(request.TariffGradeId);
        if (tariffGrade == null)
            return NotFound($"Тарифний розряд з id={request.TariffGradeId} не знайдений.");
        if (request.DismissalDate != null && request.IsPrimary)
            return BadRequest("Звільнена ставка не може бути головною.");
        if (request.IsPrimary && !position.IsPrimary)
        {
            foreach (var p in employee.Positions.Where(p => p.IsPrimary && p.DismissalDate == null && p.Id != posId))
                p.IsPrimary = false;
        }
        position.TariffGradeId = request.TariffGradeId;
        position.RateCount = request.RateCount;
        position.IsPrimary = request.IsPrimary;
        position.DismissalDate = request.DismissalDate;
        position.HasMilitaryRecord = request.HasMilitaryRecord;
        position.HasUnfavorable = request.HasUnfavorable;
        await context.SaveChangesAsync();
        var saved = await LoadFullPositionAsync(posId);
        return Ok(EmployeePositionDto.FromEntity(saved!));
    }

    /// <summary>
    /// Soft dismiss ставки — виставляє DismissalDate=today. Якщо ця була головною
    /// і у працівника є інші активні ставки — найстарша (по HireDate) автоматично
    /// стає primary. Якщо це остання активна ставка — повертає 400 (звільнюйте
    /// людину повністю через DELETE /api/employees/{id}).
    /// </summary>
    /// <param name="employeeId">Id працівника з route.</param>
    /// <param name="posId">Id ставки з route.</param>
    /// <returns>204 NoContent при успіху, 404 якщо не знайдено, 400 при спробі звільнити останню активну ставку.</returns>
    [HttpDelete("{posId}")]
    public async Task<ActionResult> DeletePosition(int employeeId, int posId)
    {
        var employee = await context.Employees
            .Include(e => e.Positions)
            .FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee == null)
            return NotFound($"Працівник з id={employeeId} не знайдений.");
        var position = employee.Positions.FirstOrDefault(p => p.Id == posId);
        if (position == null)
            return NotFound($"Ставка з id={posId} не належить працівнику.");
        if (position.DismissalDate != null)
            return BadRequest("Ставка вже звільнена.");
        var activeOthers = employee.Positions.Where(p => p.DismissalDate == null && p.Id != posId).ToList();
        if (activeOthers.Count == 0)
            return BadRequest("Це остання активна ставка. Звільнюйте людину повністю через DELETE /api/employees/{id}.");
        position.DismissalDate = DateOnly.FromDateTime(DateTime.Now);
        position.IsPrimary = false;
        if (!activeOthers.Any(p => p.IsPrimary))
        {
            var promoted = activeOthers.OrderBy(p => p.HireDate).First();
            promoted.IsPrimary = true;
        }
        await context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Upsert блока навантаження (години по класах, індивідуальні, зошити, інклюзивні).
    /// Тільки для Class 1 (вчителі) — інші класи отримають 400.
    /// Якщо блок ще не існує — створюється; якщо існує — поля перезаписуються.
    /// </summary>
    /// <param name="employeeId">Id працівника з route.</param>
    /// <param name="posId">Id ставки з route.</param>
    /// <param name="request">Дані навантаження.</param>
    /// <returns>200 з повним EmployeePositionDto після оновлення, 404/400 на помилки.</returns>
    [HttpPut("{posId}/workload")]
    public async Task<ActionResult<EmployeePositionDto>> UpdateWorkload(
        int employeeId,
        int posId,
        EmployeeWorkloadRequest request)
    {
        var position = await context.EmployeePositions
            .Include(p => p.Position)
            .Include(p => p.Workload)
            .FirstOrDefaultAsync(p => p.Id == posId && p.EmployeeId == employeeId);
        if (position == null)
            return NotFound($"Ставка з id={posId} не належить працівнику.");
        var errors = EmployeeValidator.ValidateBlocks(
            position.Position!.WorkerClass,
            hasWorkload: true, hasAdmin: false, hasNonPedagogical: false, hasGpd: false, hasPkr: false);
        if (errors != null)
            return BadRequest(errors);
        if (position.Workload == null)
        {
            var newBlock = EmployeeWorkloadRequest.FromRequest(request);
            newBlock.EmployeePositionId = posId;
            context.Add(newBlock);
        }
        else
        {
            position.Workload.Hours1To4 = request.Hours1To4;
            position.Workload.IndividualHours1To4 = request.IndividualHours1To4;
            position.Workload.Hours5To9 = request.Hours5To9;
            position.Workload.IndividualHours5To9 = request.IndividualHours5To9;
            position.Workload.Hours10To11 = request.Hours10To11;
            position.Workload.IndividualHours10To11 = request.IndividualHours10To11;
            position.Workload.NotebookHours1To4 = request.NotebookHours1To4;
            position.Workload.NotebookHours5To9 = request.NotebookHours5To9;
            position.Workload.NotebookHours10To11 = request.NotebookHours10To11;
            position.Workload.InclusiveHours1To4 = request.InclusiveHours1To4;
            position.Workload.InclusiveHours5To9 = request.InclusiveHours5To9;
            position.Workload.NotebookRateId = request.NotebookRateId;
        }
        await context.SaveChangesAsync();
        var saved = await LoadFullPositionAsync(posId);
        return Ok(EmployeePositionDto.FromEntity(saved!));
    }

    /// <summary>
    /// Upsert адмін-блока (директорство, класне керівництво, завідування кабінетом тощо).
    /// Тільки для Class 2 (адмін-педагогічний) — інші класи отримають 400.
    /// </summary>
    /// <param name="employeeId">Id працівника з route.</param>
    /// <param name="posId">Id ставки з route.</param>
    /// <param name="request">Дані адмін-блока.</param>
    /// <returns>200 з повним EmployeePositionDto, 404/400 на помилки.</returns>
    [HttpPut("{posId}/admin")]
    public async Task<ActionResult<EmployeePositionDto>> UpdateAdmin(
        int employeeId,
        int posId,
        EmployeeAdminRequest request)
    {
        var position = await context.EmployeePositions
            .Include(p => p.Position)
            .Include(p => p.Admin)
            .FirstOrDefaultAsync(p => p.Id == posId && p.EmployeeId == employeeId);
        if (position == null)
            return NotFound($"Ставка з id={posId} не належить працівнику.");
        var errors = EmployeeValidator.ValidateBlocks(
            position.Position!.WorkerClass,
            hasWorkload: false, hasAdmin: true, hasNonPedagogical: false, hasGpd: false, hasPkr: false);
        if (errors != null)
            return BadRequest(errors);
        if (position.Admin == null)
        {
            var newBlock = EmployeeAdminRequest.FromRequest(request);
            newBlock.EmployeePositionId = posId;
            context.Add(newBlock);
        }
        else
        {
            position.Admin.DirectorPct = request.DirectorPct;
            position.Admin.AdminRateCount = request.AdminRateCount;
            position.Admin.PedRateCount = request.PedRateCount;
            position.Admin.HasClassMgmt = request.HasClassMgmt;
            position.Admin.ClassGradeGroup = request.ClassGradeGroup;
            position.Admin.HasCabinet = request.HasCabinet;
            position.Admin.CabinetType = request.CabinetType;
            position.Admin.HasGym = request.HasGym;
            position.Admin.HasShootingRange = request.HasShootingRange;
            position.Admin.HasComputers = request.HasComputers;
            position.Admin.HasExtracurricular = request.HasExtracurricular;
            position.Admin.HasWebsite = request.HasWebsite;
        }
        await context.SaveChangesAsync();
        var saved = await LoadFullPositionAsync(posId);
        return Ok(EmployeePositionDto.FromEntity(saved!));
    }

    /// <summary>
    /// Upsert блока ГПД (група продовженого дня). Тільки для Class 1 і Class 2.
    /// Має власний тарифний розряд, відмінний від основного розряду ставки.
    /// </summary>
    /// <param name="employeeId">Id працівника з route.</param>
    /// <param name="posId">Id ставки з route.</param>
    /// <param name="request">Дані ГПД (години + TariffGradeId).</param>
    /// <returns>200 з повним EmployeePositionDto, 404/400 на помилки.</returns>
    [HttpPut("{posId}/gpd")]
    public async Task<ActionResult<EmployeePositionDto>> UpdateGpd(
        int employeeId,
        int posId,
        EmployeeGpdRequest request)
    {
        var position = await context.EmployeePositions
            .Include(p => p.Position)
            .Include(p => p.Gpd)
            .FirstOrDefaultAsync(p => p.Id == posId && p.EmployeeId == employeeId);
        if (position == null)
            return NotFound($"Ставка з id={posId} не належить працівнику.");
        var tariffGrade = await context.TariffGrades.FindAsync(request.TariffGradeId);
        if (tariffGrade == null)
            return NotFound($"Тарифний розряд з id={request.TariffGradeId} не знайдений.");
        var errors = EmployeeValidator.ValidateBlocks(
            position.Position!.WorkerClass,
            hasWorkload: false, hasAdmin: false, hasNonPedagogical: false, hasGpd: true, hasPkr: false);
        if (errors != null)
            return BadRequest(errors);
        if (position.Gpd == null)
        {
            var newBlock = EmployeeGpdRequest.FromRequest(request);
            newBlock.EmployeePositionId = posId;
            context.Add(newBlock);
        }
        else
        {
            position.Gpd.GpdHours = request.GpdHours;
            position.Gpd.TariffGradeId = request.TariffGradeId;
        }
        await context.SaveChangesAsync();
        var saved = await LoadFullPositionAsync(posId);
        return Ok(EmployeePositionDto.FromEntity(saved!));
    }

    /// <summary>
    /// Upsert блока ПКР (педагогічно-керівнича робота — гуртки, секції).
    /// Тільки для Class 1 і Class 2. Має власний тарифний розряд.
    /// </summary>
    /// <param name="employeeId">Id працівника з route.</param>
    /// <param name="posId">Id ставки з route.</param>
    /// <param name="request">Дані ПКР (години + TariffGradeId).</param>
    /// <returns>200 з повним EmployeePositionDto, 404/400 на помилки.</returns>
    [HttpPut("{posId}/pkr")]
    public async Task<ActionResult<EmployeePositionDto>> UpdatePkr(
        int employeeId,
        int posId,
        EmployeePkrRequest request)
    {
        var position = await context.EmployeePositions
            .Include(p => p.Position)
            .Include(p => p.Pkr)
            .FirstOrDefaultAsync(p => p.Id == posId && p.EmployeeId == employeeId);
        if (position == null)
            return NotFound($"Ставка з id={posId} не належить працівнику.");
        var tariffGrade = await context.TariffGrades.FindAsync(request.TariffGradeId);
        if (tariffGrade == null)
            return NotFound($"Тарифний розряд з id={request.TariffGradeId} не знайдений.");
        var errors = EmployeeValidator.ValidateBlocks(
            position.Position!.WorkerClass,
            hasWorkload: false, hasAdmin: false, hasNonPedagogical: false, hasGpd: false, hasPkr: true);
        if (errors != null)
            return BadRequest(errors);
        if (position.Pkr == null)
        {
            var newBlock = EmployeePkrRequest.FromRequest(request);
            newBlock.EmployeePositionId = posId;
            context.Add(newBlock);
        }
        else
        {
            position.Pkr.PkrHours = request.PkrHours;
            position.Pkr.TariffGradeId = request.TariffGradeId;
        }
        await context.SaveChangesAsync();
        var saved = await LoadFullPositionAsync(posId);
        return Ok(EmployeePositionDto.FromEntity(saved!));
    }

    /// <summary>
    /// Upsert непедагогічного блока (фіксовані суми за наставництво, бібліотеку,
    /// підручники + МОП-надбавки: нічні зміни, дезінфектанти). Тільки для Class 3 (Specialist).
    /// </summary>
    /// <param name="employeeId">Id працівника з route.</param>
    /// <param name="posId">Id ставки з route.</param>
    /// <param name="request">Дані непедагогічного блока.</param>
    /// <returns>200 з повним EmployeePositionDto, 404/400 на помилки.</returns>
    [HttpPut("{posId}/nonpedagogical")]
    public async Task<ActionResult<EmployeePositionDto>> UpdateNonPedagogical(
        int employeeId,
        int posId,
        EmployeeNonPedagogicalRequest request)
    {
        var position = await context.EmployeePositions
            .Include(p => p.Position)
            .Include(p => p.NonPedagogical)
            .FirstOrDefaultAsync(p => p.Id == posId && p.EmployeeId == employeeId);
        if (position == null)
            return NotFound($"Ставка з id={posId} не належить працівнику.");
        var errors = EmployeeValidator.ValidateBlocks(
            position.Position!.WorkerClass,
            hasWorkload: false, hasAdmin: false, hasNonPedagogical: true, hasGpd: false, hasPkr: false);
        if (errors != null)
            return BadRequest(errors);
        if (position.NonPedagogical == null)
        {
            var newBlock = EmployeeNonPedagogicalRequest.FromRequest(request);
            newBlock.EmployeePositionId = posId;
            context.Add(newBlock);
        }
        else
        {
            position.NonPedagogical.HasDisinfectants = request.HasDisinfectants;
            position.NonPedagogical.HasNightShifts = request.HasNightShifts;
            position.NonPedagogical.HasMentor = request.HasMentor;
            position.NonPedagogical.MentorAmount = request.MentorAmount;
            position.NonPedagogical.HasLibraryMgmt = request.HasLibraryMgmt;
            position.NonPedagogical.LibraryMgmtAmount = request.LibraryMgmtAmount;
            position.NonPedagogical.HasTextbooks = request.HasTextbooks;
            position.NonPedagogical.TextbooksAmount = request.TextbooksAmount;
        }
        await context.SaveChangesAsync();
        var saved = await LoadFullPositionAsync(posId);
        return Ok(EmployeePositionDto.FromEntity(saved!));
    }

    private async Task<EmployeePosition?> LoadFullPositionAsync(int posId)
    {
        return await context.EmployeePositions
            .Include(p => p.Position).ThenInclude(p => p!.Department)
            .Include(p => p.TariffGrade)
            .Include(p => p.Workload)
            .Include(p => p.Admin)
            .Include(p => p.Gpd)
            .Include(p => p.Pkr)
            .Include(p => p.NonPedagogical)
            .FirstOrDefaultAsync(p => p.Id == posId);
    }
}
