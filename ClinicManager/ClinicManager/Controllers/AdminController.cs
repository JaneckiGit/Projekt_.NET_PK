using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Models.Enums;
using ClinicManager.Models.ViewModels;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ClinicManager.Controllers;

[Authorize(Roles = Roles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext db, 
        UserManager<ApplicationUser> userManager,
        ILogger<AdminController> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> FinancialReport(
        int? patientId, 
        string? doctorId, 
        DateOnly? dateFrom, 
        DateOnly? dateTo, 
        CancellationToken ct)
    {
        var model = await BuildFinancialReportAsync(patientId, doctorId, dateFrom, dateTo, ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> FinancialReportPdf(
        int? patientId, 
        string? doctorId, 
        DateOnly? dateFrom, 
        DateOnly? dateTo, 
        CancellationToken ct)
    {
        // Enforce community license inside report action as well
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var model = await BuildFinancialReportAsync(patientId, doctorId, dateFrom, dateTo, ct);
        var document = new FinancialReportDocument(model);
        
        byte[] pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", "raport-koszty.pdf");
    }

    private async Task<FinancialReportViewModel> BuildFinancialReportAsync(
        int? patientId, 
        string? doctorId, 
        DateOnly? dateFrom, 
        DateOnly? dateTo, 
        CancellationToken ct)
    {
        // 1. Get all active patients for dropdown
        var patientsList = await _db.Patients
            .AsNoTracking()
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Pesel = p.Pesel
            })
            .ToListAsync(ct);

        // 2. Get all doctors for dropdown
        var doctorsList = (await _userManager.GetUsersInRoleAsync(Roles.Lekarz))
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .Select(d => new DoctorOptionDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName
            })
            .ToList();

        // 3. Build query
        var q = _db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Include(v => v.ProceduresPerformed)
            .Include(v => v.PrescribedMedications)
                .ThenInclude(pm => pm.Medication)
            .Where(v => v.Status != VisitStatus.Cancelled)
            .AsQueryable();

        if (patientId.HasValue && patientId.Value > 0)
        {
            q = q.Where(v => v.PatientId == patientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(doctorId))
        {
            q = q.Where(v => v.DoctorId == doctorId);
        }

        if (dateFrom.HasValue)
        {
            var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue);
            q = q.Where(v => v.ScheduledAt >= from);
        }

        if (dateTo.HasValue)
        {
            var to = dateTo.Value.ToDateTime(TimeOnly.MaxValue);
            q = q.Where(v => v.ScheduledAt <= to);
        }

        var visits = await q
            .OrderByDescending(v => v.ScheduledAt)
            .ToListAsync(ct);

        // 4. Map to item view models
        var items = visits.Select(v => new FinancialReportItemViewModel
        {
            VisitId = v.Id,
            ScheduledAt = v.ScheduledAt,
            PatientName = $"{v.Patient.LastName} {v.Patient.FirstName}",
            PatientPesel = v.Patient.Pesel,
            DoctorName = v.Doctor != null ? $"{v.Doctor.LastName} {v.Doctor.FirstName}" : "—",
            ProceduresCost = v.ProceduresPerformed.Sum(p => p.Cost),
            MedicationsCost = v.PrescribedMedications.Sum(m => m.Cost * m.Quantity),
            Procedures = v.ProceduresPerformed.Select(p => $"{p.Name} ({p.Cost:N2} zł)").ToList(),
            Medications = v.PrescribedMedications.Select(m => $"{m.Medication.Name} x{m.Quantity} ({m.Cost:N2} zł)").ToList()
        }).ToList();

        // 5. Aggregate sums
        var totalProceduresCost = items.Sum(i => i.ProceduresCost);
        var totalMedicationsCost = items.Sum(i => i.MedicationsCost);

        // 6. Grouped collections
        var groupedByPatient = visits
            .GroupBy(v => $"{v.Patient.LastName} {v.Patient.FirstName} ({v.Patient.Pesel})")
            .Select(g => new FinancialReportGroupViewModel
            {
                GroupKey = g.Key,
                ProceduresCost = g.Sum(v => v.ProceduresPerformed.Sum(p => p.Cost)),
                MedicationsCost = g.Sum(v => v.PrescribedMedications.Sum(m => m.Cost * m.Quantity))
            })
            .OrderByDescending(g => g.TotalCost)
            .ToList();

        var groupedByDoctor = visits
            .GroupBy(v => v.Doctor != null ? $"{v.Doctor.LastName} {v.Doctor.FirstName}" : "Nieprzypisany")
            .Select(g => new FinancialReportGroupViewModel
            {
                GroupKey = g.Key,
                ProceduresCost = g.Sum(v => v.ProceduresPerformed.Sum(p => p.Cost)),
                MedicationsCost = g.Sum(v => v.PrescribedMedications.Sum(m => m.Cost * m.Quantity))
            })
            .OrderByDescending(g => g.TotalCost)
            .ToList();

        var groupedByMonth = visits
            .GroupBy(v => v.ScheduledAt.ToString("yyyy-MM"))
            .Select(g => new FinancialReportGroupViewModel
            {
                GroupKey = g.Key,
                ProceduresCost = g.Sum(v => v.ProceduresPerformed.Sum(p => p.Cost)),
                MedicationsCost = g.Sum(v => v.PrescribedMedications.Sum(m => m.Cost * m.Quantity))
            })
            .OrderByDescending(g => g.GroupKey)
            .ToList();

        return new FinancialReportViewModel
        {
            PatientId = patientId,
            DoctorId = doctorId,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Patients = patientsList,
            Doctors = doctorsList,
            TotalProceduresCost = totalProceduresCost,
            TotalMedicationsCost = totalMedicationsCost,
            Items = items,
            GroupedByPatient = groupedByPatient,
            GroupedByDoctor = groupedByDoctor,
            GroupedByMonth = groupedByMonth
        };
    }
}
