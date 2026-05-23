using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = Roles.Rejestratorka + "," + Roles.Admin)]
public class PatientsController : Controller
{
    private readonly IPatientService _patients;

    public PatientsController(IPatientService patients)
    {
        _patients = patients;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken ct)
    {
        ViewData["Query"] = q;
        var list = await _patients.SearchAsync(q, ct);
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var dto = await _patients.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpGet]
    public IActionResult Create() => View(new PatientFormDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
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
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _patients.GetFormByIdAsync(id, ct);
        if (form is null) return NotFound();
        form.Id = id;
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var dto = await _patients.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        var ok = await _patients.SoftDeleteAsync(id, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Pacjent został oznaczony jako usunięty (RODO: rekord zachowany w bazie).";
        return RedirectToAction(nameof(Index));
    }
}
