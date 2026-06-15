using System.Collections.Generic;
using ClinicManager.DTOs;

namespace ClinicManager.Models.ViewModels;

public class PatientDetailsViewModel
{
    public PatientDto Patient { get; set; } = new();

    public IReadOnlyList<VisitDto> Visits { get; set; } = new List<VisitDto>();
}
