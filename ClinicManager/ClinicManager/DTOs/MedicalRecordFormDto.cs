using System.ComponentModel.DataAnnotations;
using ClinicManager.Models.Enums;

namespace ClinicManager.DTOs;

public class MedicalRecordFormDto
{
    public int PatientId { get; set; }

    [Display(Name = "Grupa krwi")]
    public BloodType BloodType { get; set; } = BloodType.Unknown;

    [StringLength(1000)]
    [Display(Name = "Alergie")]
    public string? Allergies { get; set; }

    [StringLength(2000)]
    [Display(Name = "Choroby przewlekle")]
    public string? ChronicConditions { get; set; }

    [StringLength(2000)]
    [Display(Name = "Stale leki")]
    public string? CurrentMedications { get; set; }

    [StringLength(4000)]
    [Display(Name = "Uwagi")]
    public string? Notes { get; set; }
}
