using ClinicManager.Models.Enums;

namespace ClinicManager.DTOs;

public class VisitDto
{
    public int Id { get; set; }

    public DateTime ScheduledAt { get; set; }

    public VisitStatus Status { get; set; }

    public int PatientId { get; set; }
    public string PatientFirstName { get; set; } = string.Empty;
    public string PatientLastName { get; set; } = string.Empty;
    public string PatientPesel { get; set; } = string.Empty;

    public string DoctorId { get; set; } = string.Empty;
    public string DoctorFirstName { get; set; } = string.Empty;
    public string DoctorLastName { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public string PatientDisplayName => $"{LastNameOrEmpty(PatientLastName)} {PatientFirstName}".Trim();

    public string DoctorDisplayName => $"{LastNameOrEmpty(DoctorLastName)} {DoctorFirstName}".Trim();

    private static string LastNameOrEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value;
}
