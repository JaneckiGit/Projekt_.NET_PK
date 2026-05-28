using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models;

public class ClinicalNote
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
