using ClinicManager.DTOs;
using ClinicManager.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Mappers;

[Mapper]
public partial class VisitProcedureMedicationMapper
{
    // ──────────────────── ProcedurePerformed ────────────────────

    [MapperIgnoreTarget(nameof(ProcedurePerformedDto.VisitId))]
    [MapperIgnoreSource(nameof(ProcedurePerformed.Visit))]
    [MapperIgnoreSource(nameof(ProcedurePerformed.VisitId))]
    public partial ProcedurePerformedDto ToDto(ProcedurePerformed src);

    [MapperIgnoreSource(nameof(ProcedurePerformedFormDto.Id))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.Id))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.VisitId))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.Visit))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.CreatedAt))]
    public partial ProcedurePerformed ToEntity(ProcedurePerformedFormDto src);

    [MapperIgnoreSource(nameof(ProcedurePerformedFormDto.Id))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.Id))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.VisitId))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.Visit))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.CreatedAt))]
    public partial void UpdateEntity(ProcedurePerformedFormDto src, ProcedurePerformed target);

    [MapperIgnoreSource(nameof(ProcedurePerformed.Visit))]
    [MapperIgnoreSource(nameof(ProcedurePerformed.CreatedAt))]
    [MapperIgnoreSource(nameof(ProcedurePerformed.VisitId))]
    [MapperIgnoreSource(nameof(ProcedurePerformed.Id))]
    [MapperIgnoreTarget(nameof(ProcedurePerformedFormDto.Id))]
    public partial ProcedurePerformedFormDto ToFormDto(ProcedurePerformed src);

    // ──────────────────── PrescribedMedication ────────────────────

    [MapProperty(nameof(PrescribedMedication.Medication) + "." + nameof(Medication.Name),
                 nameof(PrescribedMedicationDto.MedicationName))]
    [MapProperty(nameof(PrescribedMedication.Medication) + "." + nameof(Medication.ActiveSubstance),
                 nameof(PrescribedMedicationDto.MedicationActiveSubstance))]
    [MapProperty(nameof(PrescribedMedication.Medication) + "." + nameof(Medication.Form),
                 nameof(PrescribedMedicationDto.MedicationForm))]
    [MapperIgnoreSource(nameof(PrescribedMedication.Visit))]
    public partial PrescribedMedicationDto ToDto(PrescribedMedication src);

    [MapperIgnoreSource(nameof(PrescribedMedicationFormDto.Id))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.Id))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.VisitId))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.Visit))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.Medication))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.CreatedAt))]
    public partial PrescribedMedication ToEntity(PrescribedMedicationFormDto src);

    [MapperIgnoreSource(nameof(PrescribedMedicationFormDto.Id))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.Id))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.VisitId))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.Visit))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.Medication))]
    [MapperIgnoreTarget(nameof(PrescribedMedication.CreatedAt))]
    public partial void UpdateEntity(PrescribedMedicationFormDto src, PrescribedMedication target);

    [MapperIgnoreSource(nameof(PrescribedMedication.Visit))]
    [MapperIgnoreSource(nameof(PrescribedMedication.Medication))]
    [MapperIgnoreSource(nameof(PrescribedMedication.CreatedAt))]
    [MapperIgnoreSource(nameof(PrescribedMedication.VisitId))]
    [MapperIgnoreSource(nameof(PrescribedMedication.Id))]
    [MapperIgnoreTarget(nameof(PrescribedMedicationFormDto.Id))]
    public partial PrescribedMedicationFormDto ToFormDto(PrescribedMedication src);

    // ──────────────────── Medication ────────────────────

    [MapperIgnoreSource(nameof(Medication.PrescribedMedications))]
    public partial MedicationOptionDto ToOptionDto(Medication src);
}
