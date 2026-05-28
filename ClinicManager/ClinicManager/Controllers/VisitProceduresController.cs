using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManager.Controllers;

/// <summary>
/// Obsługuje procedury medyczne i leki przypisane do wizyty (US-07).
/// Lekarz może dodawać / edytować / usuwać wpisy dla swoich wizyt.
/// Admin i Rejestratorka mają pełen dostęp.
/// </summary>
[Authorize]
public class VisitProceduresController : Controller
{
    // Lekarz widzi i zarządza; Admin i Rejestratorka też mają dostęp
    private const string ManageRoles =
        Roles.Admin + "," + Roles.Rejestratorka + "," + Roles.Lekarz;

    private readonly IVisitProcedureMedicationService _service;
    private readonly IVisitService _visits;

    public VisitProceduresController(
        IVisitProcedureMedicationService service,
        IVisitService visits)
    {
        _service = service;
        _visits = visits;
    }

    // ─────────────────── Procedury ───────────────────

    [HttpGet]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> AddProcedure(int visitId, CancellationToken ct)
    {
        if (!await _service.VisitExistsAsync(visitId, ct)) return NotFound();
        ViewBag.VisitId = visitId;
        return View(new ProcedurePerformedFormDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> AddProcedure(int visitId, ProcedurePerformedFormDto dto, CancellationToken ct)
    {
        if (!await _service.VisitExistsAsync(visitId, ct)) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.VisitId = visitId;
            return View(dto);
        }

        await _service.AddProcedureAsync(visitId, dto, ct);
        TempData["Success"] = $"Dodano procedurę: {dto.Name}.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }

    [HttpGet]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> EditProcedure(int id, CancellationToken ct)
    {
        var form = await _service.GetProcedureFormByIdAsync(id, ct);
        if (form is null) return NotFound();

        var procedure = await _service.GetProcedureByIdAsync(id, ct);
        ViewBag.VisitId = procedure!.VisitId;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> EditProcedure(int id, int visitId, ProcedurePerformedFormDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.VisitId = visitId;
            return View(dto);
        }

        var ok = await _service.UpdateProcedureAsync(id, dto, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Zaktualizowano procedurę.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }

    [HttpGet]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> DeleteProcedure(int id, CancellationToken ct)
    {
        var dto = await _service.GetProcedureByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("DeleteProcedure")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> DeleteProcedureConfirmed(int id, int visitId, CancellationToken ct)
    {
        var ok = await _service.DeleteProcedureAsync(id, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Usunięto procedurę.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }

    // ─────────────────── Leki ───────────────────

    [HttpGet]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> AddMedication(int visitId, CancellationToken ct)
    {
        if (!await _service.VisitExistsAsync(visitId, ct)) return NotFound();
        await PopulateMedicationsAsync(ct);
        ViewBag.VisitId = visitId;
        return View(new PrescribedMedicationFormDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> AddMedication(int visitId, PrescribedMedicationFormDto dto, CancellationToken ct)
    {
        if (!await _service.VisitExistsAsync(visitId, ct)) return NotFound();

        if (dto.MedicationId > 0 && !await _service.MedicationExistsAsync(dto.MedicationId, ct))
            ModelState.AddModelError(nameof(dto.MedicationId), "Wybrany lek nie istnieje.");

        if (!ModelState.IsValid)
        {
            await PopulateMedicationsAsync(ct, dto.MedicationId);
            ViewBag.VisitId = visitId;
            return View(dto);
        }

        await _service.AddPrescribedMedicationAsync(visitId, dto, ct);
        TempData["Success"] = "Dodano lek do wizyty.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }

    [HttpGet]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> EditMedication(int id, CancellationToken ct)
    {
        var form = await _service.GetPrescribedMedicationFormByIdAsync(id, ct);
        if (form is null) return NotFound();

        var pm = await _service.GetPrescribedMedicationByIdAsync(id, ct);
        form.Id = id;
        await PopulateMedicationsAsync(ct, form.MedicationId);
        ViewBag.VisitId = pm!.VisitId;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> EditMedication(int id, int visitId, PrescribedMedicationFormDto dto, CancellationToken ct)
    {
        if (dto.MedicationId > 0 && !await _service.MedicationExistsAsync(dto.MedicationId, ct))
            ModelState.AddModelError(nameof(dto.MedicationId), "Wybrany lek nie istnieje.");

        if (!ModelState.IsValid)
        {
            await PopulateMedicationsAsync(ct, dto.MedicationId);
            ViewBag.VisitId = visitId;
            return View(dto);
        }

        var ok = await _service.UpdatePrescribedMedicationAsync(id, dto, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Zaktualizowano lek.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }

    [HttpGet]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> DeleteMedication(int id, CancellationToken ct)
    {
        var dto = await _service.GetPrescribedMedicationByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("DeleteMedication")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageRoles)]
    public async Task<IActionResult> DeleteMedicationConfirmed(int id, int visitId, CancellationToken ct)
    {
        var ok = await _service.DeletePrescribedMedicationAsync(id, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Usunięto lek z wizyty.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }

    private async Task PopulateMedicationsAsync(CancellationToken ct, int selectedId = 0)
    {
        var meds = await _service.GetMedicationsAsync(ct);
        ViewBag.Medications = new SelectList(
            meds.Select(m => new { m.Id, Display = m.DisplayName }).ToList(),
            "Id", "Display",
            selectedId > 0 ? selectedId : null);
    }
}
