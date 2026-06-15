using ClinicManager.Models.Enums;

namespace ClinicManager.DTOs;

public class MedicalRecordAccessLogDto
{
    public long Id { get; set; }
    public int MedicalRecordId { get; set; }
    public int PatientId { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public MedicalRecordAction Action { get; set; }
    public DateTime AccessedAt { get; set; }
    public string? IpAddress { get; set; }
}
