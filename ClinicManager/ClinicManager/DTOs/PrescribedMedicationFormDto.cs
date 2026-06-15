using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

public class PrescribedMedicationFormDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Wybierz lek.")]
    [Range(1, int.MaxValue, ErrorMessage = "Wybierz lek.")]
    [Display(Name = "Lek")]
    public int MedicationId { get; set; }

    [Required(ErrorMessage = "Podaj dawkowanie.")]
    [StringLength(200, ErrorMessage = "Dawkowanie może mieć maksymalnie 200 znaków.")]
    [Display(Name = "Dawkowanie")]
    public string Dosage { get; set; } = string.Empty;

    [Range(1, 9999, ErrorMessage = "Ilość musi być od 1 do 9999.")]
    [Display(Name = "Ilość")]
    public int Quantity { get; set; } = 1;

    [Range(0, 99999.99, ErrorMessage = "Koszt musi być nieujemny.")]
    [Display(Name = "Koszt (PLN)")]
    public decimal Cost { get; set; }

    [StringLength(500, ErrorMessage = "Notatka może mieć maksymalnie 500 znaków.")]
    [Display(Name = "Notatki")]
    public string? Notes { get; set; }
}
