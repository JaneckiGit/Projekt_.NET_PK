using ClinicManager.DTOs;

namespace ClinicManager.Services;

public interface IClinicalNoteService
{
    Task<IReadOnlyList<ClinicalNoteDto>> GetNotesForVisitAsync(int visitId, CancellationToken ct = default);
    Task<ClinicalNoteDto?> GetNoteByIdAsync(int id, CancellationToken ct = default);
    Task<ClinicalNoteFormDto?> GetNoteFormByIdAsync(int id, CancellationToken ct = default);
    Task<ClinicalNoteDto> CreateNoteAsync(int visitId, string content, string authorId, CancellationToken ct = default);
    Task<bool> UpdateNoteAsync(int id, string content, CancellationToken ct = default);
    Task<bool> DeleteNoteAsync(int id, CancellationToken ct = default);
    Task<bool> VisitExistsAsync(int visitId, CancellationToken ct = default);
}
