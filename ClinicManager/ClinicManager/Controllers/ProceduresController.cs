using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Controllers;

/// <summary>
/// Zarządzanie katalogiem procedur medycznych – tylko dla Admina.
/// </summary>
[Authorize(Roles = Roles.Admin)]
public class ProceduresController : Controller
{
    private readonly IVisitProcedureMedicationService _service;
    private readonly ILogger<ProceduresController> _logger;

    public ProceduresController(IVisitProcedureMedicationService service, ILogger<ProceduresController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var list = await _service.GetCatalogProceduresAsync(ct);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProcedureOptionDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProcedureOptionDto dto, CancellationToken ct)
    {
        if (await _service.CatalogProcedureNameExistsAsync(dto.Name, ct: ct))
        {
            ModelState.AddModelError(nameof(dto.Name), "Procedura o tej nazwie już istnieje w katalogu.");
        }

        if (!ModelState.IsValid) return View(dto);

        await _service.CreateCatalogProcedureAsync(dto, ct);
        TempData["Success"] = $"Dodano procedurę: {dto.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var dto = await _service.GetCatalogProcedureByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProcedureOptionDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest();

        if (await _service.CatalogProcedureNameExistsAsync(dto.Name, excludeId: id, ct: ct))
        {
            ModelState.AddModelError(nameof(dto.Name), "Procedura o tej nazwie już istnieje w katalogu.");
        }

        if (!ModelState.IsValid) return View(dto);

        var ok = await _service.UpdateCatalogProcedureAsync(id, dto, ct);
        if (!ok) return NotFound();

        TempData["Success"] = $"Zaktualizowano procedurę: {dto.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var dto = await _service.GetCatalogProcedureByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var dto = await _service.GetCatalogProcedureByIdAsync(id, ct);
        if (dto is null) return NotFound();

        try
        {
            var ok = await _service.DeleteCatalogProcedureAsync(id, ct);
            if (!ok) return NotFound();

            TempData["Success"] = $"Usunięto procedurę: {dto.Name} z katalogu.";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = $"Nie można usunąć procedury '{dto.Name}', ponieważ została już wykonana podczas wizyt pacjentów.";
        }

        return RedirectToAction(nameof(Index));
    }
}
