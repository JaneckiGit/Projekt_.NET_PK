using NBomber.Contracts.Stats;
using NBomber.CSharp;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PerformanceTests;

public static class VisitsLoadTest
{
    private const string DefaultBaseUrl = "https://localhost:5252";
    private const string ReportFolderName = "nbomber-reports";
    private const string TargetPath = "/api/visits/active";

    public static void Main()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var baseUrl = Environment.GetEnvironmentVariable("CLINIC_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = DefaultBaseUrl;
        }
        
        Directory.CreateDirectory(ReportFolderName);
        
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl)
        };

        var scenario = Scenario.Create("get_active_visits", async context =>
            {
                var response = await httpClient.GetAsync(TargetPath);

                return response.IsSuccessStatusCode
                    ? Response.Ok()
                    : Response.Fail();
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.IterationsForConstant(copies: 50, iterations: 100));

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportFolder(ReportFolderName)
            .WithReportFormats(ReportFormat.Html)
            .Run();

        SavePdfReport(stats, ReportFolderName, baseUrl);
    }
    
    private static void SavePdfReport(NodeStats stats, string folder, string baseUrl)
    {
        var scenario = stats.ScenarioStats.FirstOrDefault();
        if (scenario is null)
        {
            Console.WriteLine("No scenario stats available; skipping PDF report.");
            return;
        }

        var ok = scenario.Ok;
        var fail = scenario.Fail;
        var timestamp = DateTime.Now;
        var pdfPath = Path.Combine(folder, $"nbomber_report_{timestamp:yyyy-MM-dd--HH-mm-ss}.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text("RAPORT TESTU WYDAJNOŚCIOWEGO")
                        .Bold().FontSize(20).FontColor(Colors.Indigo.Darken3);
                    col.Item().Text($"Endpoint: GET {TargetPath}").FontSize(10);
                    col.Item().Text($"Adres bazowy: {baseUrl}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    col.Item().Text($"Scenariusz: {scenario.ScenarioName}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    col.Item().Text($"Wygenerowano: {timestamp:yyyy-MM-dd HH:mm:ss}").FontSize(9).Italic();
                });

                page.Content().PaddingVertical(0.8f, Unit.Centimetre).Column(col =>
                {
                    col.Spacing(20);

                    col.Item().Row(row =>
                    {
                        SummaryCard(row, "ŻĄDANIA (RAZEM)", scenario.AllRequestCount.ToString(), Colors.Indigo.Darken3);
                        row.ConstantItem(10);
                        SummaryCard(row, "PRZEPUSTOWOŚĆ (RPS)", $"{ok.Request.RPS:N0}", Colors.Green.Darken3);
                        row.ConstantItem(10);
                        SummaryCard(row, "BŁĘDY", scenario.AllFailCount.ToString(),
                            scenario.AllFailCount > 0 ? Colors.Red.Darken2 : Colors.Green.Darken3);
                    });

                    col.Item().Column(c =>
                    {
                        c.Spacing(5);
                        c.Item().Text("Podsumowanie").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
                        c.Item().Table(table =>
                        {
                            TwoColumns(table);
                            MetricRow(table, "Liczba żądań (razem)", scenario.AllRequestCount.ToString());
                            MetricRow(table, "Udane (OK)", scenario.AllOkCount.ToString());
                            MetricRow(table, "Nieudane (błędy)", scenario.AllFailCount.ToString());
                            MetricRow(table, "Czas trwania", scenario.Duration.ToString(@"hh\:mm\:ss"));
                            MetricRow(table, "Przepustowość (RPS, OK)", $"{ok.Request.RPS:N2}");
                            MetricRow(table, "Przesłane dane", $"{scenario.AllBytes / 1024.0:N2} KB");
                        });
                    });

                    col.Item().Column(c =>
                    {
                        c.Spacing(5);
                        c.Item().Text("Czas odpowiedzi (udane żądania)").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
                        c.Item().Table(table =>
                        {
                            TwoColumns(table);
                            MetricRow(table, "Min (ms)", $"{ok.Latency.MinMs:N2}");
                            MetricRow(table, "Średnia (ms)", $"{ok.Latency.MeanMs:N2}");
                            MetricRow(table, "Max (ms)", $"{ok.Latency.MaxMs:N2}");
                            MetricRow(table, "Odchylenie std. (ms)", $"{ok.Latency.StdDev:N2}");
                            MetricRow(table, "p50 (ms)", $"{ok.Latency.Percent50:N2}");
                            MetricRow(table, "p75 (ms)", $"{ok.Latency.Percent75:N2}");
                            MetricRow(table, "p95 (ms)", $"{ok.Latency.Percent95:N2}");
                            MetricRow(table, "p99 (ms)", $"{ok.Latency.Percent99:N2}");
                        });
                    });

                    col.Item().Column(c =>
                    {
                        c.Spacing(5);
                        c.Item().Text("Błędy (nieudane żądania)").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
                        c.Item().Table(table =>
                        {
                            TwoColumns(table);
                            MetricRow(table, "Liczba błędów", scenario.AllFailCount.ToString());
                            MetricRow(table, "Średni czas błędnej odpowiedzi (ms)", $"{fail.Latency.MeanMs:N2}");

                            var statusCodes = ok.StatusCodes
                                .Concat(fail.StatusCodes)
                                .ToArray();

                            if (statusCodes.Length == 0)
                            {
                                MetricRow(table, "Kody statusu", "brak danych");
                            }
                            else
                            {
                                foreach (var sc in statusCodes)
                                {
                                    var label = string.IsNullOrWhiteSpace(sc.StatusCode) ? "(brak)" : sc.StatusCode;
                                    MetricRow(table, $"Kod statusu {label}", $"{sc.Count}");
                                }
                            }
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("ClinicManager — test wydajnościowy NBomber  |  Strona ");
                    x.CurrentPageNumber();
                    x.Span(" z ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(pdfPath);

        Console.WriteLine($"PDF report saved: {Path.GetFullPath(pdfPath)}");
    }

    private static void SummaryCard(RowDescriptor row, string title, string value, string valueColor)
    {
        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
        {
            c.Item().Text(title).FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
            c.Item().Text(value).FontSize(16).Bold().FontColor(valueColor);
        });
    }

    private static void TwoColumns(TableDescriptor table)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(3);
            columns.RelativeColumn(2);
        });
    }

    private static void MetricRow(TableDescriptor table, string metric, string value)
    {
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(metric);
        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(value).AlignRight();
    }
}
