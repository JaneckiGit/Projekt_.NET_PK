using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

/// <summary>
/// Katalog leków dostępnych do przypisania do wizyty.
/// </summary>
public class Medication
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ActiveSubstance { get; set; }

    [MaxLength(100)]
    public string? Form { get; set; }   // tabletka, kapsułka, syrop…

    [MaxLength(50)]
    public string? DefaultDosage { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    [Range(0.00, 99999.99, ErrorMessage = "Cena jednostkowa musi być większa lub równa zero.")]
    public decimal UnitPrice { get; set; }

    public ICollection<PrescribedMedication> PrescribedMedications { get; set; } = new List<PrescribedMedication>();
}
