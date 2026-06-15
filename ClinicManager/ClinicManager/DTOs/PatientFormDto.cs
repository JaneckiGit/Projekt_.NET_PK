using System.ComponentModel.DataAnnotations;
using ClinicManager.Models.Validation;

namespace ClinicManager.DTOs;

public class PatientFormDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Imię jest wymagane.")]
    [StringLength(100)]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane.")]
    [StringLength(100)]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "PESEL jest wymagany.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "PESEL musi mieć 11 cyfr.")]
    [RegularExpression("^[0-9]{11}$", ErrorMessage = "PESEL musi składać się wyłącznie z cyfr.")]
    [Pesel(ErrorMessage = "Niepoprawny numer PESEL.")]
    [Display(Name = "PESEL")]
    public string Pesel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Numer ubezpieczenia jest wymagany.")]
    [StringLength(50)]
    [Display(Name = "Nr ubezpieczenia")]
    public string InsuranceNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data urodzenia jest wymagana.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data urodzenia")]
    public DateOnly DateOfBirth { get; set; }

    [Phone(ErrorMessage = "Niepoprawny numer telefonu.")]
    [StringLength(20)]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Niepoprawny e-mail.")]
    [StringLength(200)]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    [StringLength(250)]
    [Display(Name = "Adres")]
    public string? Address { get; set; }
}
