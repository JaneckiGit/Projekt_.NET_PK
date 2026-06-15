using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Controllers;

/// <summary>
/// Zarządzanie katalogiem leków – tylko dla Admin i Rejestratorka.
/// </summary>
[Authorize(Roles = Roles.Admin + "," + Roles.Rejestratorka)]
public class MedicationsController : Controller
{
    private readonly IVisitProcedureMedicationService _service;
    private readonly ILogger<MedicationsController> _logger;

    public MedicationsController(IVisitProcedureMedicationService service, ILogger<MedicationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _service.GetMedicationsAsync(ct);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new MedicationOptionDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicationOptionDto dto, CancellationToken ct)
    {
        if (await _service.MedicationNameExistsAsync(dto.Name, ct: ct))
        {
            ModelState.AddModelError(nameof(dto.Name), "Lek o tej nazwie już istnieje w katalogu.");
        }

        if (!ModelState.IsValid) return View(dto);

        await _service.CreateMedicationAsync(dto, ct);
        TempData["Success"] = $"Dodano lek: {dto.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var dto = await _service.GetMedicationByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MedicationOptionDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest();

        if (await _service.MedicationNameExistsAsync(dto.Name, excludeId: id, ct: ct))
        {
            ModelState.AddModelError(nameof(dto.Name), "Lek o tej nazwie już istnieje w katalogu.");
        }

        if (!ModelState.IsValid) return View(dto);

        var ok = await _service.UpdateMedicationAsync(id, dto, ct);
        if (!ok) return NotFound();

        TempData["Success"] = $"Zaktualizowano lek: {dto.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var dto = await _service.GetMedicationByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var dto = await _service.GetMedicationByIdAsync(id, ct);
        if (dto is null) return NotFound();

        try
        {
            var ok = await _service.DeleteMedicationAsync(id, ct);
            if (!ok) return NotFound();

            TempData["Success"] = $"Usunięto lek: {dto.Name} z katalogu.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"Nie można usunąć leku '{dto.Name}', ponieważ został już przepisany pacjentom w wizytach.";
        }

        return RedirectToAction(nameof(Index));
    }
}
