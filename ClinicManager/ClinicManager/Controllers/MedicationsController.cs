using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

/// <summary>
/// Zarządzanie katalogiem leków – tylko dla Admin i Rejestratorka.
/// </summary>
[Authorize(Roles = Roles.Admin + "," + Roles.Rejestratorka)]
public class MedicationsController : Controller
{
    private readonly IVisitProcedureMedicationService _service;

    public MedicationsController(IVisitProcedureMedicationService service)
    {
        _service = service;
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
        if (!ModelState.IsValid) return View(dto);

        await _service.CreateMedicationAsync(dto, ct);
        TempData["Success"] = $"Dodano lek: {dto.Name}.";
        return RedirectToAction(nameof(Index));
    }
}
