using System.Net;
using System.Net.Mail;
using ClinicManager.DTOs;

namespace ClinicManager.Services;

public class UpcomingVisitsReportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReportEmailOptions _options;
    private readonly ILogger<UpcomingVisitsReportBackgroundService> _logger;

    public UpcomingVisitsReportBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<UpcomingVisitsReportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = configuration.GetSection("ReportEmail").Get<ReportEmailOptions>()
                   ?? new ReportEmailOptions();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "UpcomingVisitsReportBackgroundService started. Interval: {Interval} min.",
            _options.IntervalMinutes);
        
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendReportAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating or sending the upcoming visits report.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task SendReportAsync(CancellationToken ct)
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        _logger.LogInformation("SendReportAsync: fetching visits for {Date}...", tomorrow);

        using var scope = _scopeFactory.CreateScope();
        var visitService = scope.ServiceProvider.GetRequiredService<IVisitService>();
        var pdfService = scope.ServiceProvider.GetRequiredService<IPdfReportService>();

        var filter = new VisitListFilterDto
        {
            DateFrom = tomorrow,
            DateTo = tomorrow
        };

        var visits = await visitService.ListAsync(filter, ct);

        using var message = new MailMessage();
        var fromAddress = !string.IsNullOrWhiteSpace(_options.SmtpUser) ? _options.SmtpUser : "noreply@przychodnia.pl";
        message.From = new MailAddress(fromAddress);
        message.To.Add(_options.AdminEmail);
        message.Subject = $"Raport wizyt na {tomorrow:yyyy-MM-dd}";
        message.IsBodyHtml = false;

        if (visits.Count == 0)
        {
            message.Body = $"Brak zaplanowanych wizyt na dzień {tomorrow:yyyy-MM-dd}.";
        }
        else
        {
            message.Body = $"W załączeniu raport wizyt zaplanowanych na {tomorrow:yyyy-MM-dd}.\n" +
                           $"Liczba wizyt: {visits.Count}.";

            var pdfBytes = pdfService.GenerateUpcomingVisitsReportPdf(visits, tomorrow);
            var fileName = $"raport-nadchodzace-wizyty_{tomorrow:yyyy-MM-dd}.pdf";
            var attachment = new Attachment(new MemoryStream(pdfBytes), fileName, "application/pdf");
            message.Attachments.Add(attachment);
        }

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort);
        if (!string.IsNullOrWhiteSpace(_options.SmtpUser))
        {
            client.Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword);
        }
        client.EnableSsl = _options.UseSsl;

        await client.SendMailAsync(message, ct);

        _logger.LogInformation(
            "Upcoming visits report for {Date} sent to {Email}. Visits: {Count}.",
            tomorrow, _options.AdminEmail, visits.Count);
    }
}
