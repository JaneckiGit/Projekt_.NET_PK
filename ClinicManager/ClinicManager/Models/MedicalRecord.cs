using System.ComponentModel.DataAnnotations;
using ClinicManager.Models.Enums;

namespace ClinicManager.Models;

public class MedicalRecord
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public BloodType BloodType { get; set; } = BloodType.Unknown;

    [MaxLength(1000)]
    public string? Allergies { get; set; }

    [MaxLength(2000)]
    public string? ChronicConditions { get; set; }

    [MaxLength(2000)]
    public string? CurrentMedications { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<MedicalEntry> Entries { get; set; } = new List<MedicalEntry>();
    public ICollection<MedicalRecordAccessLog> AccessLogs { get; set; } = new List<MedicalRecordAccessLog>();
}
