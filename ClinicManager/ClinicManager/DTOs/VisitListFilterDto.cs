using System.ComponentModel.DataAnnotations;
using ClinicManager.Models.Enums;

namespace ClinicManager.DTOs;

public class VisitListFilterDto
{
    [DataType(DataType.Date)]
    [Display(Name = "Data od")]
    public DateOnly? DateFrom { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Data do")]
    public DateOnly? DateTo { get; set; }

    [Display(Name = "Status")]
    public VisitStatus? Status { get; set; }

    [Display(Name = "Lekarz")]
    public string? DoctorId { get; set; }
}
