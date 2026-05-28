using ClinicManager.DTOs;

namespace ClinicManager.Services;

public interface IVisitProcedureMedicationService
{
    // ──── Katalog leków ────
    Task<IReadOnlyList<MedicationOptionDto>> GetMedicationsAsync(CancellationToken ct = default);
    Task<MedicationOptionDto?> GetMedicationByIdAsync(int id, CancellationToken ct = default);
    Task<MedicationOptionDto> CreateMedicationAsync(MedicationOptionDto dto, CancellationToken ct = default);

    // ──── Procedury dla wizyty ────
    Task<IReadOnlyList<ProcedurePerformedDto>> GetProceduresForVisitAsync(int visitId, CancellationToken ct = default);
    Task<ProcedurePerformedDto?> GetProcedureByIdAsync(int id, CancellationToken ct = default);
    Task<ProcedurePerformedFormDto?> GetProcedureFormByIdAsync(int id, CancellationToken ct = default);
    Task<ProcedurePerformedDto> AddProcedureAsync(int visitId, ProcedurePerformedFormDto dto, CancellationToken ct = default);
    Task<bool> UpdateProcedureAsync(int id, ProcedurePerformedFormDto dto, CancellationToken ct = default);
    Task<bool> DeleteProcedureAsync(int id, CancellationToken ct = default);

    // ──── Leki przypisane do wizyty ────
    Task<IReadOnlyList<PrescribedMedicationDto>> GetMedicationsForVisitAsync(int visitId, CancellationToken ct = default);
    Task<PrescribedMedicationDto?> GetPrescribedMedicationByIdAsync(int id, CancellationToken ct = default);
    Task<PrescribedMedicationFormDto?> GetPrescribedMedicationFormByIdAsync(int id, CancellationToken ct = default);
    Task<PrescribedMedicationDto> AddPrescribedMedicationAsync(int visitId, PrescribedMedicationFormDto dto, CancellationToken ct = default);
    Task<bool> UpdatePrescribedMedicationAsync(int id, PrescribedMedicationFormDto dto, CancellationToken ct = default);
    Task<bool> DeletePrescribedMedicationAsync(int id, CancellationToken ct = default);

    Task<bool> VisitExistsAsync(int visitId, CancellationToken ct = default);
    Task<bool> MedicationExistsAsync(int medicationId, CancellationToken ct = default);
}
