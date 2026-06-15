using System.Security.Claims;
using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize]
public class MedicalRecordsController : Controller
{
    private const string MedicalStaffRoles = Roles.Lekarz + "," + Roles.Admin;
    private const string ScanUploadRoles = Roles.Admin + "," + Roles.Lekarz;

    private readonly IMedicalRecordService _records;
    private readonly ILogger<MedicalRecordsController> _logger;

    public MedicalRecordsController(IMedicalRecordService records, ILogger<MedicalRecordsController> logger)
    {
        _records = records;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> Details(int patientId, CancellationToken ct)
    {
        var dto = await _records.GetDetailsAsync(patientId, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpGet]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> Edit(int patientId, CancellationToken ct)
    {
        var form = await _records.GetSummaryFormAsync(patientId, ct);
        if (form is null) return NotFound();
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> Edit(int patientId, MedicalRecordFormDto dto, CancellationToken ct)
    {
        if (patientId != dto.PatientId) return BadRequest();
        if (!ModelState.IsValid) return View(dto);

        var ok = await _records.UpdateSummaryAsync(patientId, dto, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Zaktualizowano kartoteke pacjenta.";
        return RedirectToAction(nameof(Details), new { patientId });
    }

    [HttpGet]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> Delete(int patientId, CancellationToken ct)
    {
        var dto = await _records.GetDetailsAsync(patientId, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> DeleteConfirmed(int patientId, CancellationToken ct)
    {
        var ok = await _records.SoftDeleteAsync(patientId, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Kartoteka oznaczona jako usunieta (RODO: dane zachowane w bazie).";
        return RedirectToAction("Details", "Patients", new { id = patientId });
    }

    [HttpGet]
    [Authorize(Roles = MedicalStaffRoles)]
    public IActionResult CreateEntry(int patientId)
    {
        var form = new MedicalEntryFormDto
        {
            PatientId = patientId,
            EntryDate = DateOnly.FromDateTime(DateTime.Today)
        };
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> CreateEntry(int patientId, MedicalEntryFormDto dto, CancellationToken ct)
    {
        if (patientId != dto.PatientId) return BadRequest();
        if (!ModelState.IsValid) return View(dto);

        var authorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(authorId)) return Forbid();

        var newId = await _records.AddEntryAsync(patientId, dto, authorId, ct);
        if (newId is null) return NotFound();

        TempData["Success"] = "Dodano wpis do kartoteki.";
        return RedirectToAction(nameof(Details), new { patientId });
    }

    [HttpGet]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> EditEntry(int entryId, CancellationToken ct)
    {
        var form = await _records.GetEntryFormAsync(entryId, ct);
        if (form is null) return NotFound();
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> EditEntry(int entryId, MedicalEntryFormDto dto, CancellationToken ct)
    {
        if (entryId != dto.Id) return BadRequest();
        if (!ModelState.IsValid) return View(dto);

        var patientId = await _records.UpdateEntryAsync(entryId, dto, ct);
        if (patientId is null) return NotFound();

        TempData["Success"] = "Zaktualizowano wpis.";
        return RedirectToAction(nameof(Details), new { patientId });
    }

    [HttpGet]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> DeleteEntry(int entryId, CancellationToken ct)
    {
        var dto = await _records.GetEntryAsync(entryId, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("DeleteEntry")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = MedicalStaffRoles)]
    public async Task<IActionResult> DeleteEntryConfirmed(int entryId, CancellationToken ct)
    {
        var patientId = await _records.DeleteEntryAsync(entryId, ct);
        if (patientId is null) return NotFound();

        TempData["Success"] = "Usunieto wpis z kartoteki.";
        return RedirectToAction(nameof(Details), new { patientId });
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AccessLog(int patientId, int page = 1, CancellationToken ct = default)
    {
        var result = await _records.GetAccessLogsAsync(patientId, page, pageSize: 50, ct);
        ViewData["PatientId"] = patientId;
        return View(result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ScanUploadRoles)]

    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<IActionResult> UploadScan(int recordId, int patientId, IFormFile? scanFile, CancellationToken ct)
    {
        try
        {
            var result = await _records.UploadScanAsync(recordId, scanFile!, ct);
            if (result is null) return NotFound();

            TempData["Success"] = "Skan dokumentu zostal przeslany.";
            return RedirectToAction(nameof(Details), new { patientId = result.PatientId });
        }
        catch (InvalidFileException ex)
        {
            TempData["UploadError"] = ex.Message;

            if (patientId > 0)
                return RedirectToAction(nameof(Details), new { patientId });

            return BadRequest(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetScan(int recordId, CancellationToken ct)
    {
        var file = await _records.GetScanAsync(recordId, ct);
        if (file is null) return NotFound();

        return PhysicalFile(file.PhysicalPath, file.ContentType, file.FileName);
    }
}
