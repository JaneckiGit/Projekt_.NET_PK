using System.Security.Claims;
using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = Roles.Admin + "," + Roles.Lekarz)]
public class ClinicalNotesController : Controller
{
    private readonly IClinicalNoteService _notesService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClinicalNotesController(IClinicalNoteService notesService, UserManager<ApplicationUser> userManager)
    {
        _notesService = notesService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Add(int visitId, CancellationToken ct)
    {
        if (!await _notesService.VisitExistsAsync(visitId, ct)) return NotFound();
        ViewBag.VisitId = visitId;
        return View(new ClinicalNoteFormDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int visitId, ClinicalNoteFormDto dto, CancellationToken ct)
    {
        if (!await _notesService.VisitExistsAsync(visitId, ct)) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.VisitId = visitId;
            return View(dto);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        await _notesService.CreateNoteAsync(visitId, dto.Content, userId, ct);
        TempData["Success"] = "Dodano notatkę kliniczną.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _notesService.GetNoteFormByIdAsync(id, ct);
        if (form is null) return NotFound();

        var note = await _notesService.GetNoteByIdAsync(id, ct);
        ViewBag.VisitId = note!.VisitId;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, int visitId, ClinicalNoteFormDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.VisitId = visitId;
            return View(dto);
        }

        var ok = await _notesService.UpdateNoteAsync(id, dto.Content, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Zaktualizowano notatkę kliniczną.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var dto = await _notesService.GetNoteByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, int visitId, CancellationToken ct)
    {
        var ok = await _notesService.DeleteNoteAsync(id, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Usunięto notatkę kliniczną.";
        return RedirectToAction("Details", "Visits", new { id = visitId });
    }
}
