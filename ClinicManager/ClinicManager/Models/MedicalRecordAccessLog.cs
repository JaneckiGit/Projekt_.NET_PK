using System.ComponentModel.DataAnnotations;
using ClinicManager.Models.Enums;

namespace ClinicManager.Models;

public class MedicalRecordAccessLog
{
    public long Id { get; set; }

    public int MedicalRecordId { get; set; }
    public MedicalRecord MedicalRecord { get; set; } = null!;

    public int PatientId { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(256)]
    public string UserName { get; set; } = string.Empty;

    public MedicalRecordAction Action { get; set; }

    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(45)]
    public string? IpAddress { get; set; }
}
