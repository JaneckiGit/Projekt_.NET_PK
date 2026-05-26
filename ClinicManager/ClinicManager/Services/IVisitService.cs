using ClinicManager.DTOs;

namespace ClinicManager.Services;

public interface IVisitService
{
    Task<IReadOnlyList<VisitDto>> ListAsync(VisitListFilterDto filter, CancellationToken ct = default);
    Task<VisitDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<VisitFormDto?> GetFormByIdAsync(int id, CancellationToken ct = default);
    Task<VisitDto> CreateAsync(VisitFormDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, VisitFormDto dto, CancellationToken ct = default);
    Task<bool> CancelAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DoctorOptionDto>> GetDoctorsAsync(CancellationToken ct = default);
    Task<bool> PatientExistsAsync(int patientId, CancellationToken ct = default);
    Task<bool> DoctorExistsAsync(string doctorId, CancellationToken ct = default);
}
