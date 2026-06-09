using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper]
public partial class VisitMapper
{
    [MapProperty(nameof(Visit.Patient) + "." + nameof(Patient.FirstName), nameof(VisitDto.PatientFirstName))]
    [MapProperty(nameof(Visit.Patient) + "." + nameof(Patient.LastName), nameof(VisitDto.PatientLastName))]
    [MapProperty(nameof(Visit.Patient) + "." + nameof(Patient.Pesel), nameof(VisitDto.PatientPesel))]
    [MapProperty(nameof(Visit.Doctor) + "." + nameof(ApplicationUser.FirstName), nameof(VisitDto.DoctorFirstName))]
    [MapProperty(nameof(Visit.Doctor) + "." + nameof(ApplicationUser.LastName), nameof(VisitDto.DoctorLastName))]
    [MapperIgnoreSource(nameof(Visit.ProceduresPerformed))]
    [MapperIgnoreSource(nameof(Visit.PrescribedMedications))]
    [MapperIgnoreSource(nameof(Visit.ClinicalNotes))]
    public partial VisitDto ToDto(Visit src);

    [MapperIgnoreSource(nameof(Visit.Status))]
    [MapperIgnoreSource(nameof(Visit.Patient))]
    [MapperIgnoreSource(nameof(Visit.Doctor))]
    [MapperIgnoreSource(nameof(Visit.CreatedAt))]
    [MapperIgnoreSource(nameof(Visit.UpdatedAt))]
    [MapperIgnoreSource(nameof(Visit.ProceduresPerformed))]
    [MapperIgnoreSource(nameof(Visit.PrescribedMedications))]
    [MapperIgnoreSource(nameof(Visit.ClinicalNotes))]
    public partial VisitFormDto ToFormDto(Visit src);

    [MapperIgnoreSource(nameof(VisitFormDto.Id))]
    [MapperIgnoreTarget(nameof(Visit.Id))]
    [MapperIgnoreTarget(nameof(Visit.Status))]
    [MapperIgnoreTarget(nameof(Visit.Patient))]
    [MapperIgnoreTarget(nameof(Visit.Doctor))]
    [MapperIgnoreTarget(nameof(Visit.CreatedAt))]
    [MapperIgnoreTarget(nameof(Visit.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Visit.ProceduresPerformed))]
    [MapperIgnoreTarget(nameof(Visit.PrescribedMedications))]
    [MapperIgnoreTarget(nameof(Visit.ClinicalNotes))]
    public partial Visit ToEntity(VisitFormDto src);

    [MapperIgnoreSource(nameof(VisitFormDto.Id))]
    [MapperIgnoreTarget(nameof(Visit.Id))]
    [MapperIgnoreTarget(nameof(Visit.Status))]
    [MapperIgnoreTarget(nameof(Visit.Patient))]
    [MapperIgnoreTarget(nameof(Visit.Doctor))]
    [MapperIgnoreTarget(nameof(Visit.CreatedAt))]
    [MapperIgnoreTarget(nameof(Visit.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Visit.ProceduresPerformed))]
    [MapperIgnoreTarget(nameof(Visit.PrescribedMedications))]
    [MapperIgnoreTarget(nameof(Visit.ClinicalNotes))]
    public partial void UpdateEntity(VisitFormDto src, Visit target);

    [MapProperty(nameof(Visit.Id), nameof(ActiveVisitDto.VisitId))]
    [MapProperty(nameof(Visit.Patient), nameof(ActiveVisitDto.PatientName), Use = nameof(MapPatientName))]
    [MapProperty(nameof(Visit.Patient) + "." + nameof(Patient.Pesel), nameof(ActiveVisitDto.PatientPesel))]
    [MapProperty(nameof(Visit.Doctor), nameof(ActiveVisitDto.DoctorName), Use = nameof(MapDoctorName))]
    [MapperIgnoreTarget(nameof(ActiveVisitDto.DoctorSpecialty))]
    [MapperIgnoreSource(nameof(Visit.PatientId))]
    [MapperIgnoreSource(nameof(Visit.DoctorId))]
    [MapperIgnoreSource(nameof(Visit.Notes))]
    [MapperIgnoreSource(nameof(Visit.CreatedAt))]
    [MapperIgnoreSource(nameof(Visit.UpdatedAt))]
    [MapperIgnoreSource(nameof(Visit.ProceduresPerformed))]
    [MapperIgnoreSource(nameof(Visit.PrescribedMedications))]
    [MapperIgnoreSource(nameof(Visit.ClinicalNotes))]
    public partial ActiveVisitDto ToActiveVisitDto(Visit src);

    private static string MapPatientName(Patient patient)
        => $"{patient.LastName} {patient.FirstName}".Trim();

    private static string MapDoctorName(ApplicationUser? doctor)
        => doctor is null ? string.Empty : $"{doctor.LastName} {doctor.FirstName}".Trim();
}
