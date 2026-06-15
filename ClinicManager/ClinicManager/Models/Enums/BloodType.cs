using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models.Enums;

public enum BloodType
{
    [Display(Name = "Nieznana")]
    Unknown = 0,

    [Display(Name = "A Rh+")]
    APositive = 1,

    [Display(Name = "A Rh-")]
    ANegative = 2,

    [Display(Name = "B Rh+")]
    BPositive = 3,

    [Display(Name = "B Rh-")]
    BNegative = 4,

    [Display(Name = "AB Rh+")]
    AbPositive = 5,

    [Display(Name = "AB Rh-")]
    AbNegative = 6,

    [Display(Name = "0 Rh+")]
    OPositive = 7,

    [Display(Name = "0 Rh-")]
    ONegative = 8
}
