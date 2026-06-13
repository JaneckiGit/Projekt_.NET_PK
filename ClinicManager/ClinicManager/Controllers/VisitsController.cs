using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using ClinicManager.Models.Enums;
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
    private const string PdfAccessRoles = Roles.Admin + "," + Roles.Lekarz;

    private readonly IVisitService _visits;
    private readonly IPatientService _patients;
    private readonly IVisitProcedureMedicationService _procedureMedService;
    private readonly IClinicalNoteService _notesService;
    private readonly IPdfReportService _pdfReportService;
    private readonly VisitMapper _mapper;
    private readonly ILogger<VisitsController> _logger;

    public VisitsController(
        IVisitService visits,
        IPatientService patients,
        IVisitProcedureMedicationService procedureMedService,
        IClinicalNoteService notesService,
        IPdfReportService pdfReportService,
        VisitMapper mapper,
        ILogger<VisitsController> logger)
    {
        _visits = visits;
        _patients = patients;
        _procedureMedService = procedureMedService;
        _notesService = notesService;
        _pdfReportService = pdfReportService;
        _mapper = mapper;
        _logger = logger;
    }
    
    [HttpGet("api/visits/active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ActiveVisitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ActiveVisitDto>>> GetActiveVisits()
    {
        var visits = await _visits.GetActiveVisitsAsync();
        var result = visits.Select(_mapper.ToActiveVisitDto).ToList();
        return Ok(result);
    }

    /// <summary>
    /// Returns all visits scheduled for today (US-16 SQL Profiler endpoint).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of today's visits with patient and doctor details.</returns>
    [HttpGet("api/visits/today")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<VisitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<VisitDto>>> GetTodayVisits(CancellationToken ct)
    {
        var visits = await _visits.GetTodayVisitsAsync(ct);
        return Ok(visits);
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

        ViewBag.Procedures = await _procedureMedService.GetProceduresForVisitAsync(id, ct);
        ViewBag.Medications = await _procedureMedService.GetMedicationsForVisitAsync(id, ct);

        if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Lekarz))
        {
            ViewBag.ClinicalNotes = await _notesService.GetNotesForVisitAsync(id, ct);
        }

        return View(dto);
    }

    [HttpGet]
    [Authorize(Roles = ManageVisitsRoles)]
    public async Task<IActionResult> Create(int? patientId, CancellationToken ct)
    {
        await PopulateFormSelectsAsync(ct, selectedPatientId: patientId);

        var form = new VisitFormDto
        {
            ScheduledAt = DateTime.Now.Date.AddDays(1).AddHours(9)
        };

        if (patientId.HasValue && await _visits.PatientExistsAsync(patientId.Value, ct))
        {
            form.PatientId = patientId.Value;
        }

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = ViewVisitsRoles)]
    public async Task<IActionResult> ChangeStatus(int id, VisitStatus status, CancellationToken ct)
    {
        if (!Enum.IsDefined(status))
        {
            TempData["Error"] = "Nieprawidłowy status wizyty.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var ok = await _visits.ChangeStatusAsync(id, status, ct);
        if (!ok) return NotFound();

        TempData["Success"] = $"Zmieniono status wizyty na: {DisplayStatus(status)}.";
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

    /// <summary>
    /// Generates and downloads a PDF visit card with prescription for the given visit.
    /// </summary>
    /// <param name="id">Visit ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>PDF file binary.</returns>
    [HttpGet]
    [Authorize(Roles = PdfAccessRoles)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken ct)
    {
        var pdfBytes = await _pdfReportService.GenerateVisitPdfAsync(id, ct);
        if (pdfBytes is null) return NotFound();

        return File(pdfBytes, "application/pdf", $"visit-{id}.pdf");
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

    private static string DisplayStatus(VisitStatus status)
    {
        var member = typeof(VisitStatus).GetMember(status.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? status.ToString();
    }
}
