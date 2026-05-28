using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

public class ProcedurePerformedFormDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Podaj nazwę procedury.")]
    [StringLength(200, ErrorMessage = "Nazwa procedury może mieć maksymalnie 200 znaków.")]
    [Display(Name = "Nazwa procedury")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Opis może mieć maksymalnie 500 znaków.")]
    [Display(Name = "Opis")]
    public string? Description { get; set; }

    [Range(0, 99999.99, ErrorMessage = "Koszt musi być nieujemny.")]
    [Display(Name = "Koszt (PLN)")]
    public decimal Cost { get; set; }
}
