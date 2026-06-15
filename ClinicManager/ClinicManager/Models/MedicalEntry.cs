using System.ComponentModel.DataAnnotations;
using ClinicManager.Models.Enums;

namespace ClinicManager.Models;

public class MedicalEntry
{
    public int Id { get; set; }

    public int MedicalRecordId { get; set; }
    public MedicalRecord MedicalRecord { get; set; } = null!;

    public DateOnly EntryDate { get; set; }

    public MedicalEntryType Type { get; set; } = MedicalEntryType.Notatka;

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
