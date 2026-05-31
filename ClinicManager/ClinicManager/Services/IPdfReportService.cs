namespace ClinicManager.Services;

public interface IPdfReportService
{
    Task<byte[]?> GenerateVisitPdfAsync(int visitId, CancellationToken ct = default);
}
