using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using ClinicManager.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly ApplicationDbContext _db;
    private readonly MedicalRecordMapper _mapper;
    private readonly IMedicalRecordAccessLogger _accessLogger;
    private readonly ILogger<MedicalRecordService> _logger;

    public MedicalRecordService(
        ApplicationDbContext db,
        MedicalRecordMapper mapper,
        IMedicalRecordAccessLogger accessLogger,
        ILogger<MedicalRecordService> logger)
    {
        _db = db;
        _mapper = mapper;
        _accessLogger = accessLogger;
        _logger = logger;
    }

    public async Task<MedicalRecordDto?> GetDetailsAsync(int patientId, CancellationToken ct = default)
    {
        var patient = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId, ct);
        if (patient is null) return null;

        var record = await _db.MedicalRecords
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.PatientId == patientId, ct);

        var actionLogged = MedicalRecordAction.View;

        if (record is null)
        {
            record = new MedicalRecord
            {
                PatientId = patientId,
                BloodType = BloodType.Unknown,
                CreatedAt = DateTime.UtcNow
            };
            _db.MedicalRecords.Add(record);
            await _db.SaveChangesAsync(ct);

            await _db.Entry(record).Reference(r => r.Patient).LoadAsync(ct);

            _logger.LogInformation("MedicalRecord auto-created for PatientId={PatientId} (Id={MedicalRecordId})",
                patientId, record.Id);

            actionLogged = MedicalRecordAction.Create;
        }

        var entries = await _db.MedicalEntries
            .Where(e => e.MedicalRecordId == record.Id)
            .Include(e => e.Author)
            .OrderByDescending(e => e.EntryDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        var dto = _mapper.ToDto(record);
        dto.Entries = entries.Select(e =>
        {
            var entryDto = _mapper.ToEntryDto(e);
            entryDto.AuthorDisplayName = FormatAuthor(e.Author);
            return entryDto;
        }).ToList();

        await _accessLogger.LogAsync(record.Id, patientId, actionLogged, ct);

        return dto;
    }

    public async Task<MedicalRecordFormDto?> GetSummaryFormAsync(int patientId, CancellationToken ct = default)
    {
        var record = await _db.MedicalRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.PatientId == patientId, ct);
        if (record is null) return null;

        var form = _mapper.ToFormDto(record);
        form.PatientId = patientId;
        return form;
    }

    public async Task<bool> UpdateSummaryAsync(int patientId, MedicalRecordFormDto dto, CancellationToken ct = default)
    {
        var record = await _db.MedicalRecords.FirstOrDefaultAsync(r => r.PatientId == patientId, ct);
        if (record is null) return false;

        var medicalRecordId = record.Id;

        _mapper.UpdateEntity(dto, record);
        record.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("MedicalRecord {MedicalRecordId} summary updated for PatientId={PatientId}",
            medicalRecordId, patientId);

        await _accessLogger.LogAsync(medicalRecordId, patientId, MedicalRecordAction.Edit, ct);

        return true;
    }

    public async Task<bool> SoftDeleteAsync(int patientId, CancellationToken ct = default)
    {
        var record = await _db.MedicalRecords.FirstOrDefaultAsync(r => r.PatientId == patientId, ct);
        if (record is null) return false;

        var medicalRecordId = record.Id;

        record.IsDeleted = true;
        record.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("MedicalRecord {MedicalRecordId} soft-deleted for PatientId={PatientId} (RODO retention)",
            medicalRecordId, patientId);

        await _accessLogger.LogAsync(medicalRecordId, patientId, MedicalRecordAction.Delete, ct);

        return true;
    }

    public async Task<int?> AddEntryAsync(int patientId, MedicalEntryFormDto dto, string authorId, CancellationToken ct = default)
    {
        var record = await _db.MedicalRecords.FirstOrDefaultAsync(r => r.PatientId == patientId, ct);
        if (record is null) return null;

        var medicalRecordId = record.Id;

        var entry = _mapper.ToEntry(dto);
        entry.MedicalRecordId = medicalRecordId;
        entry.AuthorId = authorId;
        entry.CreatedAt = DateTime.UtcNow;

        _db.MedicalEntries.Add(entry);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "MedicalEntry {EntryId} created in MedicalRecord {MedicalRecordId} (PatientId={PatientId}) by {AuthorId}",
            entry.Id, medicalRecordId, patientId, authorId);

        await _accessLogger.LogAsync(medicalRecordId, patientId, MedicalRecordAction.EntryCreate, ct);

        return entry.Id;
    }

    public async Task<MedicalEntryFormDto?> GetEntryFormAsync(int entryId, CancellationToken ct = default)
    {
        var entry = await _db.MedicalEntries
            .AsNoTracking()
            .Include(e => e.MedicalRecord)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (entry is null) return null;

        var form = _mapper.ToEntryFormDto(entry);
        form.Id = entry.Id;
        form.PatientId = entry.MedicalRecord.PatientId;
        return form;
    }

    public async Task<MedicalEntryDto?> GetEntryAsync(int entryId, CancellationToken ct = default)
    {
        var entry = await _db.MedicalEntries
            .AsNoTracking()
            .Include(e => e.MedicalRecord)
            .Include(e => e.Author)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (entry is null) return null;

        var dto = _mapper.ToEntryDto(entry);
        dto.AuthorDisplayName = FormatAuthor(entry.Author);
        return dto;
    }

    public async Task<int?> UpdateEntryAsync(int entryId, MedicalEntryFormDto dto, CancellationToken ct = default)
    {
        var entry = await _db.MedicalEntries
            .Include(e => e.MedicalRecord)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (entry is null) return null;

        var medicalRecordId = entry.MedicalRecordId;
        var patientId = entry.MedicalRecord.PatientId;

        _mapper.UpdateEntry(dto, entry);
        entry.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("MedicalEntry {EntryId} updated (PatientId={PatientId})", entryId, patientId);

        await _accessLogger.LogAsync(medicalRecordId, patientId, MedicalRecordAction.EntryEdit, ct);

        return patientId;
    }

    public async Task<int?> DeleteEntryAsync(int entryId, CancellationToken ct = default)
    {
        var entry = await _db.MedicalEntries
            .Include(e => e.MedicalRecord)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);
        if (entry is null) return null;

        var medicalRecordId = entry.MedicalRecordId;
        var patientId = entry.MedicalRecord.PatientId;

        _db.MedicalEntries.Remove(entry);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("MedicalEntry {EntryId} deleted (PatientId={PatientId})", entryId, patientId);

        await _accessLogger.LogAsync(medicalRecordId, patientId, MedicalRecordAction.EntryDelete, ct);

        return patientId;
    }

    public async Task<PagedResult<MedicalRecordAccessLogDto>> GetAccessLogsAsync(
        int patientId, int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var baseQuery = _db.MedicalRecordAccessLogs
            .AsNoTracking()
            .Where(l => l.PatientId == patientId);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderByDescending(l => l.AccessedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MedicalRecordAccessLogDto>
        {
            Items = items.Select(_mapper.ToAccessLogDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static string FormatAuthor(ApplicationUser? author)
    {
        if (author is null) return "<usuniety>";
        var fullName = $"{author.FirstName} {author.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? author.UserName ?? author.Email ?? author.Id
            : fullName;
    }
}
