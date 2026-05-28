using System;
using System.Collections.Generic;
using ClinicManager.DTOs;

namespace ClinicManager.Models.ViewModels;

public class FinancialReportViewModel
{
    // Active filters
    public int? PatientId { get; set; }
    public string? DoctorId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    // Dropdowns
    public List<PatientDto> Patients { get; set; } = new();
    public List<DoctorOptionDto> Doctors { get; set; } = new();

    // Aggregations
    public decimal TotalProceduresCost { get; set; }
    public decimal TotalMedicationsCost { get; set; }
    public decimal TotalCost => TotalProceduresCost + TotalMedicationsCost;

    // Report items
    public List<FinancialReportItemViewModel> Items { get; set; } = new();

    // Grouped summaries
    public List<FinancialReportGroupViewModel> GroupedByPatient { get; set; } = new();
    public List<FinancialReportGroupViewModel> GroupedByDoctor { get; set; } = new();
    public List<FinancialReportGroupViewModel> GroupedByMonth { get; set; } = new();
}

public class FinancialReportItemViewModel
{
    public int VisitId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPesel { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    public decimal ProceduresCost { get; set; }
    public decimal MedicationsCost { get; set; }
    public decimal TotalCost => ProceduresCost + MedicationsCost;

    public List<string> Procedures { get; set; } = new();
    public List<string> Medications { get; set; } = new();
}

public class FinancialReportGroupViewModel
{
    public string GroupKey { get; set; } = string.Empty; // e.g. Patient Name, Doctor Name, or Month "2026-05"
    public decimal ProceduresCost { get; set; }
    public decimal MedicationsCost { get; set; }
    public decimal TotalCost => ProceduresCost + MedicationsCost;
}
