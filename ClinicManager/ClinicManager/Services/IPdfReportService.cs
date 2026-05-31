namespace ClinicManager.Services;

public interface IPdfReportService
{
    Task<byte[]?> GenerateVisitPdfAsync(int visitId, CancellationToken ct = default);
    byte[] GenerateUpcomingVisitsReportPdf(IReadOnlyList<DTOs.VisitDto> visits, DateOnly reportDate);
}
