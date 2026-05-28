using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

/// <summary>
/// Katalog procedur medycznych oferowanych przez klinikę.
/// </summary>
public class Procedure
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa procedury jest wymagana.")]
    [MaxLength(200, ErrorMessage = "Nazwa procedury nie może przekraczać 200 znaków.")]
    [Display(Name = "Nazwa procedury")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "Opis procedury nie może przekraczać 1000 znaków.")]
    [Display(Name = "Opis procedury")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Koszt świadczenia jest wymagany.")]
    [Column(TypeName = "decimal(10,2)")]
    [Range(0.00, 99999.99, ErrorMessage = "Koszt świadczenia musi być większy lub równy zero.")]
    [Display(Name = "Koszt (PLN)")]
    public decimal Cost { get; set; }

    public ICollection<ProcedurePerformed> ProceduresPerformed { get; set; } = new List<ProcedurePerformed>();
}
