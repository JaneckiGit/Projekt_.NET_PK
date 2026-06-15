using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class PatientService : IPatientService
{
    private readonly ApplicationDbContext _db;
    private readonly PatientMapper _mapper;
    private readonly ILogger<PatientService> _logger;

    public PatientService(
        ApplicationDbContext db,
        PatientMapper mapper,
        ILogger<PatientService> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PatientDto>> SearchAsync(string? query, CancellationToken ct = default)
    {
        var q = _db.Patients.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var trimmed = query.Trim();
            q = q.Where(p =>
                EF.Functions.Like(p.LastName, trimmed + "%") ||
                EF.Functions.Like(p.FirstName, trimmed + "%") ||
                p.Pesel.StartsWith(trimmed));
        }

        var entities = await q
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(ct);

        return entities.Select(_mapper.ToDto).ToList();
    }

    public async Task<PatientDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? null : _mapper.ToDto(entity);
    }

    public async Task<PatientFormDto?> GetFormByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? null : _mapper.ToFormDto(entity);
    }

    public async Task<PatientDto> CreateAsync(PatientFormDto dto, CancellationToken ct = default)
    {
        var entity = _mapper.ToEntity(dto);
        entity.CreatedAt = DateTime.UtcNow;

        _db.Patients.Add(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientId} created (Pesel hash: {PeselHash})",
            entity.Id, entity.Pesel.GetHashCode());

        return _mapper.ToDto(entity);
    }

    public async Task<bool> UpdateAsync(int id, PatientFormDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return false;

        _mapper.UpdateEntity(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientId} updated", entity.Id);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return false;

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Patient {PatientId} soft-deleted (RODO retention)", entity.Id);
        return true;
    }

    public async Task<bool> PeselExistsAsync(string pesel, int? excludeId = null, CancellationToken ct = default)
    {
        var q = _db.Patients.AsNoTracking().Where(p => p.Pesel == pesel);
        if (excludeId.HasValue) q = q.Where(p => p.Id != excludeId.Value);
        return await q.AnyAsync(ct);
    }
}
