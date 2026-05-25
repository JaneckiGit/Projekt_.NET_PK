using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize]
public class PatientsController : Controller
{
    private const string ManagePatientsRoles = Roles.Rejestratorka + "," + Roles.Admin;
    private const string ScanUploadRoles = Roles.Admin + "," + Roles.Rejestratorka;

    private readonly IPatientService _patients;
    private readonly IMedicalRecordService _records;

    public PatientsController(IPatientService patients, IMedicalRecordService records)
    {
        _patients = patients;
        _records = records;
    }

    [HttpGet]
    [Authorize(Roles = ManagePatientsRoles + "," + Roles.Lekarz)]
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        ViewData["Query"] = q;
        var list = await _patients.SearchAsync(q, ct);
        return View(list);
    }

    [HttpGet]
    [Authorize(Roles = ManagePatientsRoles + "," + Roles.Lekarz)]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var dto = await _patients.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpGet]
    [Authorize(Roles = ManagePatientsRoles)]
    public IActionResult Create() => View(new PatientFormDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManagePatientsRoles)]
    public async Task<IActionResult> Create(PatientFormDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(dto);

        if (await _patients.PeselExistsAsync(dto.Pesel, ct: ct))
        {
            ModelState.AddModelError(nameof(dto.Pesel), "Pacjent o tym numerze PESEL już istnieje.");
            return View(dto);
        }

        var created = await _patients.CreateAsync(dto, ct);
        TempData["Success"] = $"Dodano pacjenta: {created.FirstName} {created.LastName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = ManagePatientsRoles)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _patients.GetFormByIdAsync(id, ct);
        if (form is null) return NotFound();
        form.Id = id;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManagePatientsRoles)]
    public async Task<IActionResult> Edit(int id, PatientFormDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest();
        if (!ModelState.IsValid) return View(dto);

        if (await _patients.PeselExistsAsync(dto.Pesel, excludeId: id, ct: ct))
        {
            ModelState.AddModelError(nameof(dto.Pesel), "Pacjent o tym numerze PESEL już istnieje.");
            return View(dto);
        }

        var ok = await _patients.UpdateAsync(id, dto, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Zaktualizowano pacjenta.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = ManagePatientsRoles)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var dto = await _patients.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManagePatientsRoles)]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var ok = await _patients.SoftDeleteAsync(id, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Pacjent został oznaczony jako usunięty (RODO: rekord zachowany w bazie).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ScanUploadRoles)]

    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<IActionResult> UploadScan(int id, IFormFile? scanFile, CancellationToken ct)
    {
        try
        {
            var result = await _records.UploadScanForPatientAsync(id, scanFile!, ct);
            if (result is null) return NotFound();

            TempData["Success"] = "Skan dokumentu zostal przeslany.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidFileException ex)
        {
            TempData["UploadError"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
