using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

public class ClinicalNoteFormDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Treść notatki jest wymagana.")]
    [StringLength(4000, ErrorMessage = "Treść notatki nie może przekraczać 4000 znaków.")]
    [Display(Name = "Treść notatki")]
    public string Content { get; set; } = string.Empty;
}
