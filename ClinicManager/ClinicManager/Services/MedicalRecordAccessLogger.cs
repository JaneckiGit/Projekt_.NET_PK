using System.Security.Claims;
using ClinicManager.Data;
using ClinicManager.Models;
using ClinicManager.Models.Enums;

namespace ClinicManager.Services;

public class MedicalRecordAccessLogger : IMedicalRecordAccessLogger
{
    private static readonly EventId RodoAccessEventId = new(7001, "RodoAccess");
    private static readonly EventId RodoLogFailedEventId = new(7002, "RodoLogFailed");

    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MedicalRecordAccessLogger> _logger;

    public MedicalRecordAccessLogger(
        ApplicationDbContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MedicalRecordAccessLogger> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(int medicalRecordId, int patientId, MedicalRecordAction action, CancellationToken ct = default)
    {
        try
        {
            var http = _httpContextAccessor.HttpContext;
            var user = http?.User;

            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = user?.Identity?.Name ?? "<anonymous>";
            var ip = http?.Connection?.RemoteIpAddress?.ToString();

            if (ip is { Length: > 45 })
            {
                ip = ip[..45];
            }

            var entry = new MedicalRecordAccessLog
            {
                MedicalRecordId = medicalRecordId,
                PatientId = patientId,
                UserId = userId,
                UserName = userName.Length > 256 ? userName[..256] : userName,
                Action = action,
                AccessedAt = DateTime.UtcNow,
                IpAddress = ip
            };

            _db.MedicalRecordAccessLogs.Add(entry);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                RodoAccessEventId,
                "RODO access: User={UserName} (Id={UserId}) Action={Action} PatientId={PatientId} MedicalRecordId={MedicalRecordId} Ip={Ip}",
                userName, userId, action, patientId, medicalRecordId, ip);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                RodoLogFailedEventId,
                ex,
                "Failed to persist RODO access log for PatientId={PatientId} MedicalRecordId={MedicalRecordId} Action={Action}",
                patientId, medicalRecordId, action);
        }
    }
}
