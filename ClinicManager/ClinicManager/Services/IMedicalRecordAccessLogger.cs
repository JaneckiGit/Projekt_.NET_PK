using ClinicManager.Models.Enums;

namespace ClinicManager.Services;

public interface IMedicalRecordAccessLogger
{
    Task LogAsync(int medicalRecordId, int patientId, MedicalRecordAction action, CancellationToken ct = default);
}
