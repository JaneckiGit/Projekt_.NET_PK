using ClinicManager.Models.Enums;

namespace ClinicManager.DTOs;

public class MedicalRecordDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public string PatientPesel { get; set; } = string.Empty;

    public BloodType BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }
    public string? CurrentMedications { get; set; }
    public string? Notes { get; set; }

    public string? DocumentScanUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public IReadOnlyList<MedicalEntryDto> Entries { get; set; } = Array.Empty<MedicalEntryDto>();
}
