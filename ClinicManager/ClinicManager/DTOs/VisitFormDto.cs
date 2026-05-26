using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

public class VisitFormDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Termin wizyty jest wymagany.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Termin wizyty")]
    public DateTime ScheduledAt { get; set; }

    [Required(ErrorMessage = "Wybierz pacjenta.")]
    [Range(1, int.MaxValue, ErrorMessage = "Wybierz pacjenta.")]
    [Display(Name = "Pacjent")]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Wybierz lekarza.")]
    [Display(Name = "Lekarz")]
    public string DoctorId { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Notatki mogą mieć maksymalnie 2000 znaków.")]
    [Display(Name = "Notatki")]
    public string? Notes { get; set; }
}
