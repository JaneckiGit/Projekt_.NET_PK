using ClinicManager.Models.Enums;

namespace ClinicManager.DTOs;

public class MedicalEntryDto
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public int PatientId { get; set; }
    public DateOnly EntryDate { get; set; }
    public MedicalEntryType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
