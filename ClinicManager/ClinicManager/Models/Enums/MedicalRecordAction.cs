using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models.Enums;

public enum MedicalRecordAction
{
    [Display(Name = "Podglad")]
    View = 0,

    [Display(Name = "Utworzenie kartoteki")]
    Create = 1,

    [Display(Name = "Edycja kartoteki")]
    Edit = 2,

    [Display(Name = "Usuniecie kartoteki")]
    Delete = 3,

    [Display(Name = "Dodanie wpisu")]
    EntryCreate = 4,

    [Display(Name = "Edycja wpisu")]
    EntryEdit = 5,

    [Display(Name = "Usuniecie wpisu")]
    EntryDelete = 6
}
