using System;

namespace ClinicManager.DTOs;

public class ClinicalNoteDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
