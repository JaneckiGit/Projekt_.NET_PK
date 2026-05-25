using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper]
public partial class MedicalRecordMapper
{
    [MapperIgnoreSource(nameof(MedicalRecord.Entries))]
    [MapperIgnoreSource(nameof(MedicalRecord.AccessLogs))]
    [MapperIgnoreSource(nameof(MedicalRecord.IsDeleted))]
    [MapperIgnoreSource(nameof(MedicalRecord.DeletedAt))]
    [MapProperty(nameof(MedicalRecord.Patient) + "." + nameof(Patient.FirstName), nameof(MedicalRecordDto.PatientFirstName))]
    [MapProperty(nameof(MedicalRecord.Patient) + "." + nameof(Patient.LastName), nameof(MedicalRecordDto.PatientLastName))]
    [MapProperty(nameof(MedicalRecord.Patient) + "." + nameof(Patient.Pesel), nameof(MedicalRecordDto.PatientPesel))]
    [MapperIgnoreTarget(nameof(MedicalRecordDto.Entries))]
    public partial MedicalRecordDto ToDto(MedicalRecord src);

    [MapperIgnoreSource(nameof(MedicalRecord.Id))]
    [MapperIgnoreSource(nameof(MedicalRecord.Patient))]
    [MapperIgnoreSource(nameof(MedicalRecord.Entries))]
    [MapperIgnoreSource(nameof(MedicalRecord.AccessLogs))]
    [MapperIgnoreSource(nameof(MedicalRecord.IsDeleted))]
    [MapperIgnoreSource(nameof(MedicalRecord.DeletedAt))]
    [MapperIgnoreSource(nameof(MedicalRecord.CreatedAt))]
    [MapperIgnoreSource(nameof(MedicalRecord.UpdatedAt))]
    [MapperIgnoreSource(nameof(MedicalRecord.DocumentScanUrl))]
    public partial MedicalRecordFormDto ToFormDto(MedicalRecord src);

    [MapperIgnoreSource(nameof(MedicalRecordFormDto.PatientId))]
    [MapperIgnoreTarget(nameof(MedicalRecord.Id))]
    [MapperIgnoreTarget(nameof(MedicalRecord.PatientId))]
    [MapperIgnoreTarget(nameof(MedicalRecord.Patient))]
    [MapperIgnoreTarget(nameof(MedicalRecord.Entries))]
    [MapperIgnoreTarget(nameof(MedicalRecord.AccessLogs))]
    [MapperIgnoreTarget(nameof(MedicalRecord.IsDeleted))]
    [MapperIgnoreTarget(nameof(MedicalRecord.DeletedAt))]
    [MapperIgnoreTarget(nameof(MedicalRecord.CreatedAt))]
    [MapperIgnoreTarget(nameof(MedicalRecord.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalRecord.DocumentScanUrl))]
    public partial void UpdateEntity(MedicalRecordFormDto src, MedicalRecord target);

    [MapperIgnoreSource(nameof(MedicalEntry.Author))]
    [MapProperty(nameof(MedicalEntry.MedicalRecord) + "." + nameof(MedicalRecord.PatientId), nameof(MedicalEntryDto.PatientId))]
    [MapperIgnoreTarget(nameof(MedicalEntryDto.AuthorDisplayName))]
    public partial MedicalEntryDto ToEntryDto(MedicalEntry src);

    [MapperIgnoreSource(nameof(MedicalEntryFormDto.Id))]
    [MapperIgnoreSource(nameof(MedicalEntryFormDto.PatientId))]
    [MapperIgnoreTarget(nameof(MedicalEntry.Id))]
    [MapperIgnoreTarget(nameof(MedicalEntry.MedicalRecordId))]
    [MapperIgnoreTarget(nameof(MedicalEntry.MedicalRecord))]
    [MapperIgnoreTarget(nameof(MedicalEntry.AuthorId))]
    [MapperIgnoreTarget(nameof(MedicalEntry.Author))]
    [MapperIgnoreTarget(nameof(MedicalEntry.CreatedAt))]
    [MapperIgnoreTarget(nameof(MedicalEntry.UpdatedAt))]
    public partial MedicalEntry ToEntry(MedicalEntryFormDto src);

    [MapperIgnoreSource(nameof(MedicalEntry.Id))]
    [MapperIgnoreSource(nameof(MedicalEntry.MedicalRecordId))]
    [MapperIgnoreSource(nameof(MedicalEntry.MedicalRecord))]
    [MapperIgnoreSource(nameof(MedicalEntry.AuthorId))]
    [MapperIgnoreSource(nameof(MedicalEntry.Author))]
    [MapperIgnoreSource(nameof(MedicalEntry.CreatedAt))]
    [MapperIgnoreSource(nameof(MedicalEntry.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalEntryFormDto.Id))]
    [MapperIgnoreTarget(nameof(MedicalEntryFormDto.PatientId))]
    public partial MedicalEntryFormDto ToEntryFormDto(MedicalEntry src);

    [MapperIgnoreSource(nameof(MedicalEntryFormDto.Id))]
    [MapperIgnoreSource(nameof(MedicalEntryFormDto.PatientId))]
    [MapperIgnoreTarget(nameof(MedicalEntry.Id))]
    [MapperIgnoreTarget(nameof(MedicalEntry.MedicalRecordId))]
    [MapperIgnoreTarget(nameof(MedicalEntry.MedicalRecord))]
    [MapperIgnoreTarget(nameof(MedicalEntry.AuthorId))]
    [MapperIgnoreTarget(nameof(MedicalEntry.Author))]
    [MapperIgnoreTarget(nameof(MedicalEntry.CreatedAt))]
    [MapperIgnoreTarget(nameof(MedicalEntry.UpdatedAt))]
    public partial void UpdateEntry(MedicalEntryFormDto src, MedicalEntry target);

    [MapperIgnoreSource(nameof(MedicalRecordAccessLog.MedicalRecord))]
    public partial MedicalRecordAccessLogDto ToAccessLogDto(MedicalRecordAccessLog src);
}
