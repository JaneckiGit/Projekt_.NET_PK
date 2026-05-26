using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManager.Controllers;

[Authorize]
public class VisitsController : Controller
{
    private const string ManageVisitsRoles = Roles.Admin + "," + Roles.Rejestratorka;
    private const string ViewVisitsRoles = Roles.Admin + "," + Roles.Rejestratorka + "," + Roles.Lekarz;

    private readonly IVisitService _visits;
    private readonly IPatientService _patients;

    public VisitsController(IVisitService visits, IPatientService patients)
    {
        _visits = visits;
        _patients = patients;
    }

    [HttpGet]
    [Authorize(Roles = ViewVisitsRoles)]
    public async Task<IActionResult> Index(VisitListFilterDto filter, CancellationToken ct)
    {
        await PopulateDoctorsAsync(ct, filter.DoctorId);
        ViewData["Filter"] = filter;

        var list = await _visits.ListAsync(filter, ct);
        return View(list);
    }

    [HttpGet]
    [Authorize(Roles = ViewVisitsRoles)]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var dto = await _visits.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpGet]
    [Authorize(Roles = ManageVisitsRoles)]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        await PopulateFormSelectsAsync(ct);

        var form = new VisitFormDto
        {
            ScheduledAt = DateTime.Now.Date.AddDays(1).AddHours(9)
        };
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageVisitsRoles)]
    public async Task<IActionResult> Create(VisitFormDto dto, CancellationToken ct)
    {
        await ValidateRelationsAsync(dto, ct);

        if (!ModelState.IsValid)
        {
            await PopulateFormSelectsAsync(ct, dto.DoctorId, dto.PatientId);
            return View(dto);
        }

        var created = await _visits.CreateAsync(dto, ct);
        TempData["Success"] =
            $"Zaplanowano wizytę: {created.PatientDisplayName} ({created.ScheduledAt:yyyy-MM-dd HH:mm}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = ManageVisitsRoles)]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var form = await _visits.GetFormByIdAsync(id, ct);
        if (form is null) return NotFound();

        await PopulateFormSelectsAsync(ct, form.DoctorId, form.PatientId);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageVisitsRoles)]
    public async Task<IActionResult> Edit(int id, VisitFormDto dto, CancellationToken ct)
    {
        if (id != dto.Id) return BadRequest();

        await ValidateRelationsAsync(dto, ct);

        if (!ModelState.IsValid)
        {
            await PopulateFormSelectsAsync(ct, dto.DoctorId, dto.PatientId);
            return View(dto);
        }

        var ok = await _visits.UpdateAsync(id, dto, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Zaktualizowano wizytę.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Roles = ManageVisitsRoles)]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var dto = await _visits.GetByIdAsync(id, ct);
        if (dto is null) return NotFound();
        return View(dto);
    }

    [HttpPost, ActionName("Cancel")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ManageVisitsRoles)]
    public async Task<IActionResult> CancelConfirmed(int id, CancellationToken ct)
    {
        var ok = await _visits.CancelAsync(id, ct);
        if (!ok) return NotFound();

        TempData["Success"] = "Wizyta została anulowana (soft delete: rekord zachowany w bazie).";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDoctorsAsync(CancellationToken ct, string? selectedDoctorId = null)
    {
        var doctors = await _visits.GetDoctorsAsync(ct);
        ViewBag.Doctors = new SelectList(
            doctors.Select(d => new { d.Id, Display = d.DisplayName }).ToList(),
            "Id",
            "Display",
            selectedDoctorId);
    }

    private async Task PopulateFormSelectsAsync(
        CancellationToken ct,
        string? selectedDoctorId = null,
        int? selectedPatientId = null)
    {
        await PopulateDoctorsAsync(ct, selectedDoctorId);

        var patients = await _patients.SearchAsync(null, ct);
        ViewBag.Patients = new SelectList(
            patients.Select(p => new
            {
                p.Id,
                Display = $"{p.LastName} {p.FirstName} ({p.Pesel})"
            }).ToList(),
            "Id",
            "Display",
            selectedPatientId);
    }

    private async Task ValidateRelationsAsync(VisitFormDto dto, CancellationToken ct)
    {
        if (dto.PatientId > 0 && !await _visits.PatientExistsAsync(dto.PatientId, ct))
        {
            ModelState.AddModelError(nameof(dto.PatientId), "Wybrany pacjent nie istnieje.");
        }

        if (!string.IsNullOrWhiteSpace(dto.DoctorId) && !await _visits.DoctorExistsAsync(dto.DoctorId, ct))
        {
            ModelState.AddModelError(nameof(dto.DoctorId), "Wybrany użytkownik nie jest lekarzem.");
        }
    }
}
