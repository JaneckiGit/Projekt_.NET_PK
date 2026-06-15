using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using ClinicManager.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class VisitService : IVisitService
{
    private readonly ApplicationDbContext _db;
    private readonly VisitMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<VisitService> _logger;

    public VisitService(
        ApplicationDbContext db,
        VisitMapper mapper,
        UserManager<ApplicationUser> userManager,
        ILogger<VisitService> logger)
    {
        _db = db;
        _mapper = mapper;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IReadOnlyList<VisitDto>> ListAsync(VisitListFilterDto filter, CancellationToken ct = default)
    {
        var q = _db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .AsQueryable();

        if (filter.DateFrom.HasValue)
        {
            var from = filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            q = q.Where(v => v.ScheduledAt >= from);
        }

        if (filter.DateTo.HasValue)
        {
            var to = filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue);
            q = q.Where(v => v.ScheduledAt <= to);
        }

        if (filter.Status.HasValue)
        {
            q = q.Where(v => v.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.DoctorId))
        {
            var doctorId = filter.DoctorId;
            q = q.Where(v => v.DoctorId == doctorId);
        }

        var entities = await q
            .OrderByDescending(v => v.ScheduledAt)
            .ToListAsync(ct);

        return entities.Select(_mapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<VisitDto>> GetForPatientAsync(int patientId, CancellationToken ct = default)
    {
        var entities = await _db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.ScheduledAt)
            .ToListAsync(ct);

        return entities.Select(_mapper.ToDto).ToList();
    }

    public async Task<IEnumerable<Visit>> GetActiveVisitsAsync()
    {
        return await _db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => v.Status == VisitStatus.Planned || v.Status == VisitStatus.InProgress)
            .OrderBy(v => v.ScheduledAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<VisitDto>> GetTodayVisitsAsync(CancellationToken ct = default)
    {
        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1);

        var entities = await _db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => v.ScheduledAt >= todayStart && v.ScheduledAt < todayEnd)
            .OrderBy(v => v.ScheduledAt)
            .ToListAsync(ct);

        return entities.Select(_mapper.ToDto).ToList();
    }

    public async Task<VisitDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .FirstOrDefaultAsync(v => v.Id == id, ct);

        return entity is null ? null : _mapper.ToDto(entity);
    }

    public async Task<VisitFormDto?> GetFormByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Visits.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null) return null;

        var form = _mapper.ToFormDto(entity);
        form.Id = entity.Id;
        return form;
    }

    public async Task<VisitDto> CreateAsync(VisitFormDto dto, CancellationToken ct = default)
    {
        var entity = _mapper.ToEntity(dto);
        entity.Status = VisitStatus.Planned;
        entity.CreatedAt = DateTime.UtcNow;

        _db.Visits.Add(entity);
        await _db.SaveChangesAsync(ct);

        await _db.Entry(entity).Reference(v => v.Patient).LoadAsync(ct);
        await _db.Entry(entity).Reference(v => v.Doctor).LoadAsync(ct);

        _logger.LogInformation(
            "Visit {VisitId} created for patient {PatientId} with doctor {DoctorId}",
            entity.Id, entity.PatientId, entity.DoctorId);

        return _mapper.ToDto(entity);
    }

    public async Task<bool> UpdateAsync(int id, VisitFormDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Visits.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null) return false;

        _mapper.UpdateEntity(dto, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Visit {VisitId} updated", entity.Id);
        return true;
    }

    public async Task<bool> ChangeStatusAsync(int id, VisitStatus status, CancellationToken ct = default)
    {
        var entity = await _db.Visits.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null) return false;

        var previousStatus = entity.Status;
        entity.Status = status;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Visit {VisitId} status changed from {PreviousStatus} to {NewStatus}",
            entity.Id, previousStatus, status);

        return true;
    }

    public async Task<bool> CancelAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Visits.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null) return false;

        entity.Status = VisitStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Visit {VisitId} cancelled (soft delete)", entity.Id);
        return true;
    }

    public async Task<IReadOnlyList<DoctorOptionDto>> GetDoctorsAsync(CancellationToken ct = default)
    {
        var doctors = await _userManager.GetUsersInRoleAsync(Roles.Lekarz);

        return doctors
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .Select(d => new DoctorOptionDto
            {
                Id = d.Id,
                FirstName = d.FirstName,
                LastName = d.LastName
            })
            .ToList();
    }

    public Task<bool> PatientExistsAsync(int patientId, CancellationToken ct = default)
        => _db.Patients.AnyAsync(p => p.Id == patientId, ct);

    public async Task<bool> DoctorExistsAsync(string doctorId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(doctorId)) return false;

        var user = await _userManager.FindByIdAsync(doctorId);
        if (user is null) return false;

        return await _userManager.IsInRoleAsync(user, Roles.Lekarz);
    }
}
