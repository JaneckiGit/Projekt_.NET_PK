using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper]
public partial class PatientMapper
{
    [MapperIgnoreSource(nameof(Patient.IsDeleted))]
    [MapperIgnoreSource(nameof(Patient.DeletedAt))]
    public partial PatientDto ToDto(Patient src);

    [MapperIgnoreSource(nameof(PatientFormDto.Id))]
    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    [MapperIgnoreTarget(nameof(Patient.DeletedAt))]
    [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedAt))]
    public partial Patient ToEntity(PatientFormDto src);

    [MapperIgnoreSource(nameof(PatientFormDto.Id))]
    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    [MapperIgnoreTarget(nameof(Patient.DeletedAt))]
    [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedAt))]
    public partial void UpdateEntity(PatientFormDto src, Patient target);

    [MapperIgnoreSource(nameof(Patient.IsDeleted))]
    [MapperIgnoreSource(nameof(Patient.DeletedAt))]
    [MapperIgnoreSource(nameof(Patient.CreatedAt))]
    [MapperIgnoreSource(nameof(Patient.UpdatedAt))]
    public partial PatientFormDto ToFormDto(Patient src);
}
