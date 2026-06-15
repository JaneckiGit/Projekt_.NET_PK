using System.ComponentModel.DataAnnotations;
using ClinicManager.DTOs.Validation;
using ClinicManager.Models.Enums;

namespace ClinicManager.DTOs;

public class MedicalEntryFormDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Data wpisu jest wymagana.")]
    [DataType(DataType.Date)]
    [NotInFuture(ErrorMessage = "Data wpisu nie moze byc z przyszlosci.")]
    [Display(Name = "Data wpisu")]
    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Typ wpisu jest wymagany.")]
    [Display(Name = "Typ wpisu")]
    public MedicalEntryType Type { get; set; } = MedicalEntryType.Notatka;

    [Required(ErrorMessage = "Tytul jest wymagany.")]
    [StringLength(200)]
    [Display(Name = "Tytul")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tresc jest wymagana.")]
    [StringLength(4000)]
    [Display(Name = "Tresc")]
    public string Content { get; set; } = string.Empty;
}
