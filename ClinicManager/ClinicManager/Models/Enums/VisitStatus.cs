using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models.Enums;

public enum VisitStatus
{
    [Display(Name = "Zaplanowana")]
    Planned = 0,

    [Display(Name = "W trakcie")]
    InProgress = 1,

    [Display(Name = "Zakończona")]
    Completed = 2,

    [Display(Name = "Anulowana")]
    Cancelled = 3
}
