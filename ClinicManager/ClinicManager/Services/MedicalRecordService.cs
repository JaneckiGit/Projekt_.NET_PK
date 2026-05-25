using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using ClinicManager.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private const long MaxScanFileSizeBytes = 10L * 1024L * 1024L;

    private static readonly string[] AllowedScanExtensions =
        { ".jpg", ".jpeg", ".png", ".pdf" };

    // First-byte signatures for each accepted format.
    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngMagic  = { 0x89, 0x50, 0x4E, 0x47 };
    private static readonly byte[] PdfMagic  = { 0x25, 0x50, 0x44, 0x46 };

    private readonly ApplicationDbContext _db;
    private readonly MedicalRecordMapper _mapper;
    private readonly IMedicalRecordAccessLogger _accessLogger;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MedicalRecordService> _logger;

    public MedicalRecordService(
        ApplicationDbContext db,
        MedicalRecordMapper mapper,
        IMedicalRecordAccessLogger accessLogger,
        IWebHostEnvironment env,
        ILogger<MedicalRecordService> logger)
    {
        _db = db;
        _mapper = mapper;
        _accessLogger = accessLogger;
        _env = env;
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

    public async Task<ScanUploadResultDto?> UploadScanAsync(int recordId, IFormFile scanFile, CancellationToken ct = default)
    {
        var record = await _db.MedicalRecords.FirstOrDefaultAsync(r => r.Id == recordId, ct);
        if (record is null) return null;

        return await SaveScanToRecordAsync(record, scanFile, ct);
    }

    public async Task<ScanUploadResultDto?> UploadScanForPatientAsync(int patientId, IFormFile scanFile, CancellationToken ct = default)
    {
        var patientExists = await _db.Patients.AnyAsync(p => p.Id == patientId, ct);
        if (!patientExists) return null;

        var record = await _db.MedicalRecords.FirstOrDefaultAsync(r => r.PatientId == patientId, ct);
        if (record is null)
        {
            // Auto-create MedicalRecord for this patient (same pattern as GetDetailsAsync).
            record = new MedicalRecord
            {
                PatientId = patientId,
                BloodType = BloodType.Unknown,
                CreatedAt = DateTime.UtcNow
            };
            _db.MedicalRecords.Add(record);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "MedicalRecord auto-created for PatientId={PatientId} (Id={MedicalRecordId}) during scan upload",
                patientId, record.Id);

            await _accessLogger.LogAsync(record.Id, patientId, MedicalRecordAction.Create, ct);
        }

        return await SaveScanToRecordAsync(record, scanFile, ct);
    }

    private async Task<ScanUploadResultDto> SaveScanToRecordAsync(MedicalRecord record, IFormFile scanFile, CancellationToken ct)
    {
        if (scanFile is null || scanFile.Length == 0)
            throw new InvalidFileException("Plik nie zostal przeslany lub jest pusty.");

        if (scanFile.Length > MaxScanFileSizeBytes)
            throw new InvalidFileException("Plik jest zbyt duzy. Maksymalny rozmiar to 10 MB.");

        var extension = Path.GetExtension(scanFile.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedScanExtensions.Contains(extension))
            throw new InvalidFileException("Niedozwolony typ pliku. Dozwolone rozszerzenia: .jpg, .jpeg, .png, .pdf.");

        await using (var probeStream = scanFile.OpenReadStream())
        {
            if (!await MagicBytesMatchAsync(probeStream, extension, ct))
                throw new InvalidFileException("Zawartosc pliku nie odpowiada deklarowanemu rozszerzeniu.");
        }

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
            throw new InvalidOperationException("Brak skonfigurowanego WebRootPath.");

        var uploadPath = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        Directory.CreateDirectory(uploadPath);

        var safeName = Guid.NewGuid().ToString("N") + extension;
        var filePath = Path.GetFullPath(Path.Combine(uploadPath, safeName));

        // Defense in depth: even though `safeName` is generated server-side, verify
        // that the resolved absolute path is rooted in the uploads directory.
        var uploadPathWithSeparator = uploadPath.EndsWith(Path.DirectorySeparatorChar)
            ? uploadPath
            : uploadPath + Path.DirectorySeparatorChar;
        if (!filePath.StartsWith(uploadPathWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidFileException("Nieprawidlowa sciezka docelowa pliku.");

        await using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await scanFile.CopyToAsync(output, ct);
        }

        var relativeUrl = "/uploads/" + safeName;
        record.DocumentScanUrl = relativeUrl;
        record.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Document scan uploaded for MedicalRecord {MedicalRecordId} (PatientId={PatientId}), stored at {Path}",
            record.Id, record.PatientId, relativeUrl);

        await _accessLogger.LogAsync(record.Id, record.PatientId, MedicalRecordAction.Edit, ct);

        return new ScanUploadResultDto
        {
            PatientId = record.PatientId,
            RelativeUrl = relativeUrl
        };
    }

    public async Task<ScanFileDto?> GetScanAsync(int recordId, CancellationToken ct = default)
    {
        var record = await _db.MedicalRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recordId, ct);
        if (record is null || string.IsNullOrWhiteSpace(record.DocumentScanUrl))
            return null;

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot)) return null;

        var uploadPath = Path.GetFullPath(Path.Combine(webRoot, "uploads"));
        var relative = record.DocumentScanUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.GetFullPath(Path.Combine(webRoot, relative));

        var uploadPathWithSeparator = uploadPath.EndsWith(Path.DirectorySeparatorChar)
            ? uploadPath
            : uploadPath + Path.DirectorySeparatorChar;
        if (!physicalPath.StartsWith(uploadPathWithSeparator, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!File.Exists(physicalPath)) return null;

        var fileName = Path.GetFileName(physicalPath);
        return new ScanFileDto
        {
            PhysicalPath = physicalPath,
            ContentType = GetContentType(Path.GetExtension(physicalPath).ToLowerInvariant()),
            FileName = fileName
        };
    }

    private static async Task<bool> MagicBytesMatchAsync(Stream stream, string extension, CancellationToken ct)
    {
        var expected = extension switch
        {
            ".jpg" or ".jpeg" => JpegMagic,
            ".png"            => PngMagic,
            ".pdf"            => PdfMagic,
            _                 => null
        };
        if (expected is null) return false;

        var buffer = new byte[expected.Length];
        var read = 0;
        while (read < buffer.Length)
        {
            var n = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), ct);
            if (n == 0) return false;
            read += n;
        }

        for (var i = 0; i < expected.Length; i++)
        {
            if (buffer[i] != expected[i]) return false;
        }
        return true;
    }

    private static string GetContentType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png"            => "image/png",
        ".pdf"            => "application/pdf",
        _                 => "application/octet-stream"
    };

    private static string FormatAuthor(ApplicationUser? author)
    {
        if (author is null) return "<usuniety>";
        var fullName = $"{author.FirstName} {author.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? author.UserName ?? author.Email ?? author.Id
            : fullName;
    }
}
