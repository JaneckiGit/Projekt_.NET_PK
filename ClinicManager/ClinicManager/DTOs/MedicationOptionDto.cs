using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

public class MedicationOptionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa leku jest wymagana.")]
    [StringLength(200, ErrorMessage = "Nazwa leku nie może przekraczać 200 znaków.")]
    [Display(Name = "Nazwa leku")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Substancja czynna nie może przekraczać 100 znaków.")]
    [Display(Name = "Substancja czynna")]
    public string? ActiveSubstance { get; set; }

    [StringLength(100, ErrorMessage = "Postać leku nie może przekraczać 100 znaków.")]
    [Display(Name = "Postać")]
    public string? Form { get; set; }

    [StringLength(50, ErrorMessage = "Domyślne dawkowanie nie może przekraczać 50 znaków.")]
    [Display(Name = "Domyślne dawkowanie")]
    public string? DefaultDosage { get; set; }

    [Required(ErrorMessage = "Cena jednostkowa jest wymagana.")]
    [Range(0.00, 99999.99, ErrorMessage = "Cena jednostkowa musi być większa lub równa zero.")]
    [Display(Name = "Cena jednostkowa (PLN)")]
    public decimal UnitPrice { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(ActiveSubstance)
        ? Name
        : $"{Name} ({ActiveSubstance})";
}
