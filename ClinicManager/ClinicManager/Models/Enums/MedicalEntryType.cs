using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models.Enums;

public enum MedicalEntryType
{
    [Display(Name = "Notatka")]
    Notatka = 0,

    [Display(Name = "Diagnoza")]
    Diagnoza = 1,

    [Display(Name = "Recepta")]
    Recepta = 2,

    [Display(Name = "Wynik badania")]
    WynikBadania = 3,

    [Display(Name = "Inne")]
    Inne = 4
}
