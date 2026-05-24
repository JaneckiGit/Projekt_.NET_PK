using ClinicManager.DTOs;

namespace ClinicManager.Services;

public interface IMedicalRecordService
{
    Task<MedicalRecordDto?> GetDetailsAsync(int patientId, CancellationToken ct = default);
    Task<MedicalRecordFormDto?> GetSummaryFormAsync(int patientId, CancellationToken ct = default);
    Task<bool> UpdateSummaryAsync(int patientId, MedicalRecordFormDto dto, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int patientId, CancellationToken ct = default);

    Task<int?> AddEntryAsync(int patientId, MedicalEntryFormDto dto, string authorId, CancellationToken ct = default);
    Task<MedicalEntryFormDto?> GetEntryFormAsync(int entryId, CancellationToken ct = default);
    Task<MedicalEntryDto?> GetEntryAsync(int entryId, CancellationToken ct = default);
    Task<int?> UpdateEntryAsync(int entryId, MedicalEntryFormDto dto, CancellationToken ct = default);
    Task<int?> DeleteEntryAsync(int entryId, CancellationToken ct = default);

    Task<PagedResult<MedicalRecordAccessLogDto>> GetAccessLogsAsync(int patientId, int page, int pageSize, CancellationToken ct = default);
}
