using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

/// <summary>
/// DTO dla procedury medycznej w katalogu.
/// </summary>
public class ProcedureOptionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa procedury jest wymagana.")]
    [StringLength(200, ErrorMessage = "Nazwa procedury może mieć maksymalnie 200 znaków.")]
    [Display(Name = "Nazwa procedury")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Opis może mieć maksymalnie 1000 znaków.")]
    [Display(Name = "Opis procedury")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Koszt świadczenia jest wymagany.")]
    [Range(0.00, 99999.99, ErrorMessage = "Koszt świadczenia musi być większy lub równy zero.")]
    [Display(Name = "Koszt (PLN)")]
    public decimal Cost { get; set; }

    public string DisplayName => $"{Name} - {Cost:F2} PLN";
}
