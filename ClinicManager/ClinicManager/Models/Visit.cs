using System.ComponentModel.DataAnnotations;
using ClinicManager.Models.Enums;

namespace ClinicManager.Models;

public class Visit
{
    public int Id { get; set; }

    public DateTime ScheduledAt { get; set; }

    public VisitStatus Status { get; set; } = VisitStatus.Planned;

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public string DoctorId { get; set; } = string.Empty;
    public ApplicationUser? Doctor { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
