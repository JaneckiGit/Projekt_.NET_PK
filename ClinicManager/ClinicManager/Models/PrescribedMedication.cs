using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

/// <summary>
/// Lek przepisany pacjentowi podczas konkretnej wizyty.
/// </summary>
public class PrescribedMedication
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    /// <summary>Dawkowanie np. "500mg 2x dziennie".</summary>
    [Required]
    [MaxLength(200)]
    public string Dosage { get; set; } = string.Empty;

    /// <summary>Ilość opakowań / tabletek.</summary>
    [Range(1, 9999)]
    public int Quantity { get; set; } = 1;

    /// <summary>Koszt leku w PLN (0 jeśli nieznany).</summary>
    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 99999.99)]
    public decimal Cost { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
