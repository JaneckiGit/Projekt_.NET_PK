using ClinicManager.DTOs;

namespace ClinicManager.Services;

public interface IPatientService
{
    Task<IReadOnlyList<PatientDto>> SearchAsync(string? query, CancellationToken ct = default);
    Task<PatientDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PatientFormDto?> GetFormByIdAsync(int id, CancellationToken ct = default);
    Task<PatientDto> CreateAsync(PatientFormDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, PatientFormDto dto, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
    Task<bool> PeselExistsAsync(string pesel, int? excludeId = null, CancellationToken ct = default);
}
