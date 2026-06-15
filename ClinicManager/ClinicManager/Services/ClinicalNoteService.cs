using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class ClinicalNoteService : IClinicalNoteService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ClinicalNoteService> _logger;

    public ClinicalNoteService(ApplicationDbContext db, ILogger<ClinicalNoteService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ClinicalNoteDto>> GetNotesForVisitAsync(int visitId, CancellationToken ct = default)
    {
        return await _db.ClinicalNotes
            .AsNoTracking()
            .Where(n => n.VisitId == visitId)
            .Include(n => n.Author)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new ClinicalNoteDto
            {
                Id = n.Id,
                VisitId = n.VisitId,
                Content = n.Content,
                AuthorId = n.AuthorId,
                AuthorDisplayName = n.Author != null ? $"{n.Author.LastName} {n.Author.FirstName}" : "Nieznany",
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<ClinicalNoteDto?> GetNoteByIdAsync(int id, CancellationToken ct = default)
    {
        var note = await _db.ClinicalNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (note == null) return null;

        return new ClinicalNoteDto
        {
            Id = note.Id,
            VisitId = note.VisitId,
            Content = note.Content,
            AuthorId = note.AuthorId,
            AuthorDisplayName = note.Author != null ? $"{note.Author.LastName} {note.Author.FirstName}" : "Nieznany",
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }

    public async Task<ClinicalNoteFormDto?> GetNoteFormByIdAsync(int id, CancellationToken ct = default)
    {
        var note = await _db.ClinicalNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (note == null) return null;

        return new ClinicalNoteFormDto
        {
            Id = note.Id,
            Content = note.Content
        };
    }

    public async Task<ClinicalNoteDto> CreateNoteAsync(int visitId, string content, string authorId, CancellationToken ct = default)
    {
        var entity = new ClinicalNote
        {
            VisitId = visitId,
            Content = content,
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ClinicalNotes.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Load Author navigation
        await _db.Entry(entity).Reference(n => n.Author).LoadAsync(ct);

        _logger.LogInformation("ClinicalNote {NoteId} created for visit {VisitId} by author {AuthorId}", entity.Id, visitId, authorId);

        return new ClinicalNoteDto
        {
            Id = entity.Id,
            VisitId = entity.VisitId,
            Content = entity.Content,
            AuthorId = entity.AuthorId,
            AuthorDisplayName = entity.Author != null ? $"{entity.Author.LastName} {entity.Author.FirstName}" : "Nieznany",
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public async Task<bool> UpdateNoteAsync(int id, string content, CancellationToken ct = default)
    {
        var entity = await _db.ClinicalNotes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity == null) return false;

        entity.Content = content;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("ClinicalNote {NoteId} updated", id);
        return true;
    }

    public async Task<bool> DeleteNoteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ClinicalNotes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity == null) return false;

        _db.ClinicalNotes.Remove(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("ClinicalNote {NoteId} deleted", id);
        return true;
    }

    public async Task<bool> VisitExistsAsync(int visitId, CancellationToken ct = default)
    {
        return await _db.Visits.AnyAsync(v => v.Id == visitId, ct);
    }
}
