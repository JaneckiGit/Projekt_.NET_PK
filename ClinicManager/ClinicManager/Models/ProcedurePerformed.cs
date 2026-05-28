using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

/// <summary>
/// Procedura medyczna wykonana podczas wizyty.
/// Wybierana z katalogu (Procedura).
/// </summary>
public class ProcedurePerformed
{
    public int Id { get; set; }

    public int VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public int? ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }

    /// <summary>Nazwa procedury – lekarz wpisuje lub wybiera z podpowiedzi.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Koszt procedury w PLN (0 jeśli nieznany).</summary>
    [Column(TypeName = "decimal(10,2)")]
    [Range(0, 99999.99)]
    public decimal Cost { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
