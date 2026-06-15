using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class VisitProcedureMedicationService : IVisitProcedureMedicationService
{
    private readonly ApplicationDbContext _db;
    private readonly VisitProcedureMedicationMapper _mapper;
    private readonly ILogger<VisitProcedureMedicationService> _logger;

    public VisitProcedureMedicationService(
        ApplicationDbContext db,
        VisitProcedureMedicationMapper mapper,
        ILogger<VisitProcedureMedicationService> logger)
    {
        _db = db;
        _mapper = mapper;
        _logger = logger;
    }

    // ──────────────────── Katalog leków ────────────────────

    public async Task<IReadOnlyList<MedicationOptionDto>> GetMedicationsAsync(CancellationToken ct = default)
    {
        var list = await _db.Medications
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync(ct);

        return list.Select(_mapper.ToOptionDto).ToList();
    }

    public async Task<MedicationOptionDto?> GetMedicationByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Medications.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
        return entity is null ? null : _mapper.ToOptionDto(entity);
    }

    public async Task<MedicationOptionDto> CreateMedicationAsync(MedicationOptionDto dto, CancellationToken ct = default)
    {
        var entity = new Medication
        {
            Name = dto.Name.Trim(),
            ActiveSubstance = dto.ActiveSubstance?.Trim(),
            Form = dto.Form?.Trim(),
            DefaultDosage = dto.DefaultDosage?.Trim(),
            UnitPrice = dto.UnitPrice
        };
        _db.Medications.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Medication {MedicationId} '{Name}' created", entity.Id, entity.Name);
        return _mapper.ToOptionDto(entity);
    }

    public async Task<bool> MedicationNameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var q = _db.Medications.AsNoTracking().Where(m => m.Name.ToLower() == name.Trim().ToLower());
        if (excludeId.HasValue)
        {
            q = q.Where(m => m.Id != excludeId.Value);
        }
        return await q.AnyAsync(ct);
    }

    public async Task<bool> UpdateMedicationAsync(int id, MedicationOptionDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Medications.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return false;

        entity.Name = dto.Name.Trim();
        entity.ActiveSubstance = dto.ActiveSubstance?.Trim();
        entity.Form = dto.Form?.Trim();
        entity.DefaultDosage = dto.DefaultDosage?.Trim();
        entity.UnitPrice = dto.UnitPrice;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Medication {MedicationId} updated", id);
        return true;
    }

    public async Task<bool> DeleteMedicationAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Medications.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity is null) return false;

        _db.Medications.Remove(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Medication {MedicationId} deleted", id);
        return true;
    }

    // ──────────────────── Procedury ────────────────────

    public async Task<IReadOnlyList<ProcedurePerformedDto>> GetProceduresForVisitAsync(int visitId, CancellationToken ct = default)
    {
        var list = await _db.ProceduresPerformed
            .AsNoTracking()
            .Where(p => p.VisitId == visitId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct);

        return list.Select(p =>
        {
            var dto = _mapper.ToDto(p);
            dto.VisitId = p.VisitId;
            return dto;
        }).ToList();
    }

    public async Task<ProcedurePerformedDto?> GetProcedureByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ProceduresPerformed.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return null;
        var dto = _mapper.ToDto(entity);
        dto.VisitId = entity.VisitId;
        return dto;
    }

    public async Task<ProcedurePerformedFormDto?> GetProcedureFormByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ProceduresPerformed.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return null;
        var form = _mapper.ToFormDto(entity);
        form.Id = entity.Id;
        return form;
    }

    public async Task<ProcedurePerformedDto> AddProcedureAsync(int visitId, ProcedurePerformedFormDto dto, CancellationToken ct = default)
    {
        var entity = _mapper.ToEntity(dto);
        entity.VisitId = visitId;
        entity.CreatedAt = DateTime.UtcNow;

        _db.ProceduresPerformed.Add(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Procedure {ProcedureId} added to visit {VisitId}", entity.Id, visitId);

        var result = _mapper.ToDto(entity);
        result.VisitId = entity.VisitId;
        return result;
    }

    public async Task<bool> UpdateProcedureAsync(int id, ProcedurePerformedFormDto dto, CancellationToken ct = default)
    {
        var entity = await _db.ProceduresPerformed.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return false;

        _mapper.UpdateEntity(dto, entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Procedure {ProcedureId} updated", id);
        return true;
    }

    public async Task<bool> DeleteProcedureAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.ProceduresPerformed.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return false;

        _db.ProceduresPerformed.Remove(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Procedure {ProcedureId} deleted from visit {VisitId}", id, entity.VisitId);
        return true;
    }

    // ──────────────────── Leki przypisane do wizyty ────────────────────

    public async Task<IReadOnlyList<PrescribedMedicationDto>> GetMedicationsForVisitAsync(int visitId, CancellationToken ct = default)
    {
        var list = await _db.PrescribedMedications
            .AsNoTracking()
            .Include(pm => pm.Medication)
            .Where(pm => pm.VisitId == visitId)
            .OrderBy(pm => pm.CreatedAt)
            .ToListAsync(ct);

        return list.Select(_mapper.ToDto).ToList();
    }

    public async Task<PrescribedMedicationDto?> GetPrescribedMedicationByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.PrescribedMedications
            .AsNoTracking()
            .Include(pm => pm.Medication)
            .FirstOrDefaultAsync(pm => pm.Id == id, ct);

        return entity is null ? null : _mapper.ToDto(entity);
    }

    public async Task<PrescribedMedicationFormDto?> GetPrescribedMedicationFormByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.PrescribedMedications.AsNoTracking().FirstOrDefaultAsync(pm => pm.Id == id, ct);
        if (entity is null) return null;
        var form = _mapper.ToFormDto(entity);
        form.Id = entity.Id;
        return form;
    }

    public async Task<PrescribedMedicationDto> AddPrescribedMedicationAsync(int visitId, PrescribedMedicationFormDto dto, CancellationToken ct = default)
    {
        var entity = _mapper.ToEntity(dto);
        entity.VisitId = visitId;
        entity.CreatedAt = DateTime.UtcNow;

        _db.PrescribedMedications.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(entity).Reference(pm => pm.Medication).LoadAsync(ct);

        _logger.LogInformation("PrescribedMedication {Id} added to visit {VisitId}", entity.Id, visitId);

        return _mapper.ToDto(entity);
    }

    public async Task<bool> UpdatePrescribedMedicationAsync(int id, PrescribedMedicationFormDto dto, CancellationToken ct = default)
    {
        var entity = await _db.PrescribedMedications.FirstOrDefaultAsync(pm => pm.Id == id, ct);
        if (entity is null) return false;

        _mapper.UpdateEntity(dto, entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("PrescribedMedication {Id} updated", id);
        return true;
    }

    public async Task<bool> DeletePrescribedMedicationAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.PrescribedMedications.FirstOrDefaultAsync(pm => pm.Id == id, ct);
        if (entity is null) return false;

        _db.PrescribedMedications.Remove(entity);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("PrescribedMedication {Id} deleted from visit {VisitId}", id, entity.VisitId);
        return true;
    }

    // ──────────────────── Helpery ────────────────────

    public Task<bool> VisitExistsAsync(int visitId, CancellationToken ct = default)
        => _db.Visits.AnyAsync(v => v.Id == visitId, ct);

    public Task<bool> MedicationExistsAsync(int medicationId, CancellationToken ct = default)
        => _db.Medications.AnyAsync(m => m.Id == medicationId, ct);

    // ──────────────────── Katalog procedur ────────────────────

    public async Task<IReadOnlyList<ProcedureOptionDto>> GetCatalogProceduresAsync(CancellationToken ct = default)
    {
        var list = await _db.Procedures
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(ct);

        return list.Select(_mapper.ToOptionDto).ToList();
    }

    public async Task<ProcedureOptionDto?> GetCatalogProcedureByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Procedures.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        return entity is null ? null : _mapper.ToOptionDto(entity);
    }

    public async Task<ProcedureOptionDto> CreateCatalogProcedureAsync(ProcedureOptionDto dto, CancellationToken ct = default)
    {
        var entity = new Procedure
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Cost = dto.Cost
        };
        _db.Procedures.Add(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Procedure {ProcedureId} '{Name}' created in catalog", entity.Id, entity.Name);
        return _mapper.ToOptionDto(entity);
    }

    public async Task<bool> CatalogProcedureNameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var q = _db.Procedures.AsNoTracking().Where(p => p.Name.ToLower() == name.Trim().ToLower());
        if (excludeId.HasValue)
        {
            q = q.Where(p => p.Id != excludeId.Value);
        }
        return await q.AnyAsync(ct);
    }

    public async Task<bool> UpdateCatalogProcedureAsync(int id, ProcedureOptionDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Procedures.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return false;

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim();
        entity.Cost = dto.Cost;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Procedure {ProcedureId} updated in catalog", id);
        return true;
    }

    public async Task<bool> DeleteCatalogProcedureAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Procedures.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (entity is null) return false;

        _db.Procedures.Remove(entity);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Procedure {ProcedureId} deleted from catalog", id);
        return true;
    }

    public Task<bool> CatalogProcedureExistsAsync(int id, CancellationToken ct = default)
        => _db.Procedures.AnyAsync(p => p.Id == id, ct);
}
