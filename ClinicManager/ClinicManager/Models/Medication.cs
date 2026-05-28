using System.ComponentModel.DataAnnotations;

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

    public ICollection<PrescribedMedication> PrescribedMedications { get; set; } = new List<PrescribedMedication>();
}
