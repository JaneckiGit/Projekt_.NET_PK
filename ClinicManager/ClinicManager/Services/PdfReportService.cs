using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ClinicManager.DTOs;
using ClinicManager.Models.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Services;

public class PdfReportService : IPdfReportService
{
    private readonly IVisitService _visits;
    private readonly IVisitProcedureMedicationService _procedureMedService;
    private readonly IClinicalNoteService _notesService;
    private readonly ILogger<PdfReportService> _logger;

    public PdfReportService(
        IVisitService visits,
        IVisitProcedureMedicationService procedureMedService,
        IClinicalNoteService notesService,
        ILogger<PdfReportService> logger)
    {
        _visits = visits;
        _procedureMedService = procedureMedService;
        _notesService = notesService;
        _logger = logger;
    }
    
    public async Task<byte[]?> GenerateVisitPdfAsync(int visitId, CancellationToken ct = default)
    {
        var visit = await _visits.GetByIdAsync(visitId, ct);
        if (visit is null)
        {
            _logger.LogWarning("Visit {VisitId} not found – cannot generate PDF.", visitId);
            return null;
        }

        var procedures = await _procedureMedService.GetProceduresForVisitAsync(visitId, ct);
        var medications = await _procedureMedService.GetMedicationsForVisitAsync(visitId, ct);
        var clinicalNotes = await _notesService.GetNotesForVisitAsync(visitId, ct);

        var document = new VisitPdfDocument(visit, procedures, medications, clinicalNotes);
        var pdfBytes = document.GeneratePdf();

        _logger.LogInformation(
            "PDF generated for visit {VisitId} ({Bytes} bytes).", visitId, pdfBytes.Length);

        return pdfBytes;
    }

    public byte[] GenerateUpcomingVisitsReportPdf(IReadOnlyList<VisitDto> visits, DateOnly reportDate)
    {
        var document = new UpcomingVisitsReportDocument(visits, reportDate);
        var pdfBytes = document.GeneratePdf();

        _logger.LogInformation(
            "Upcoming visits report PDF generated for {Date} ({Count} visits, {Bytes} bytes).",
            reportDate, visits.Count, pdfBytes.Length);

        return pdfBytes;
    }
    

    private sealed class VisitPdfDocument : IDocument
    {
        private readonly VisitDto _visit;
        private readonly IReadOnlyList<ProcedurePerformedDto> _procedures;
        private readonly IReadOnlyList<PrescribedMedicationDto> _medications;
        private readonly IReadOnlyList<ClinicalNoteDto> _notes;

        public VisitPdfDocument(
            VisitDto visit,
            IReadOnlyList<ProcedurePerformedDto> procedures,
            IReadOnlyList<PrescribedMedicationDto> medications,
            IReadOnlyList<ClinicalNoteDto> notes)
        {
            _visit = visit;
            _procedures = procedures;
            _medications = medications;
            _notes = notes;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        //Header

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("KARTA WIZYTY").Bold().FontSize(22)
                        .FontColor(Colors.Indigo.Darken3);
                    col.Item().Text($"Wygenerowano: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).Italic();
                });

                row.ConstantItem(120).AlignRight().Column(col =>
                {
                    col.Item().Text("ClinicManager").Bold().FontSize(16)
                        .FontColor(Colors.Indigo.Medium);
                    col.Item().Text("Zarządzanie Kliniką").FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                });
            });
        }

        //Content

        private void ComposeContent(IContainer container)
        {
            container.PaddingVertical(0.8f, Unit.Centimetre).Column(col =>
            {
                col.Spacing(20);

                ComposePatientSection(col);
                ComposeVisitInfoSection(col);
                ComposeProceduresSection(col);
                ComposeClinicalNotesSection(col);
                ComposePrescriptionSection(col);
            });
        }

        //Patient data

        private void ComposePatientSection(ColumnDescriptor col)
        {
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                {
                    c.Item().Text("DANE PACJENTA").FontSize(8).Bold()
                        .FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(4).Text($"Imię i nazwisko: {_visit.PatientDisplayName}")
                        .FontSize(11);
                    c.Item().Text($"PESEL: {_visit.PatientPesel}").FontSize(11);
                });
        }

        //Visit info

        private void ComposeVisitInfoSection(ColumnDescriptor col)
        {
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                {
                    c.Item().Text("INFORMACJE O WIZYCIE").FontSize(8).Bold()
                        .FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(4)
                        .Text($"Data wizyty: {_visit.ScheduledAt:yyyy-MM-dd HH:mm}").FontSize(11);
                    c.Item().Text($"Lekarz prowadzący: {_visit.DoctorDisplayName}").FontSize(11);
                    c.Item().Text($"Status: {DisplayName(_visit.Status)}").FontSize(11);
                });
        }

        private static string DisplayName(Enum value)
        {
            var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<DisplayAttribute>();
            return attr?.Name ?? value.ToString();
        }

        //Procedures

        private void ComposeProceduresSection(ColumnDescriptor col)
        {
            col.Item().Column(section =>
            {
                section.Spacing(5);
                section.Item().Text("Wykonane procedury").Bold().FontSize(12)
                    .FontColor(Colors.Indigo.Darken2);

                section.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); //Name
                        columns.RelativeColumn(4); //Description
                        columns.RelativeColumn(1); //Cost
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                            .Text("Nazwa").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                            .Text("Opis").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                            .Text("Koszt").Bold().FontColor(Colors.White).AlignRight();
                    });

                    if (_procedures.Count == 0)
                    {
                        table.Cell().ColumnSpan(3).BorderBottom(1)
                            .BorderColor(Colors.Grey.Lighten3).Padding(5)
                            .Text("Brak procedur").Italic();
                    }
                    else
                    {
                        foreach (var p in _procedures)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(5).Text(p.Name);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(5).Text(p.Description ?? "—");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(5).Text($"{p.Cost:N2} zł").AlignRight();
                        }
                    }
                });
            });
        }

        //Clinical notes

        private void ComposeClinicalNotesSection(ColumnDescriptor col)
        {
            col.Item().Column(section =>
            {
                section.Spacing(5);
                section.Item().Text("Notatki kliniczne").Bold().FontSize(12)
                    .FontColor(Colors.Indigo.Darken2);

                if (_notes.Count == 0)
                {
                    section.Item().PaddingLeft(5).Text("Brak notatek klinicznych.").Italic();
                }
                else
                {
                    foreach (var n in _notes)
                    {
                        section.Item().Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Padding(8).Column(noteCol =>
                            {
                                noteCol.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Autor: {n.AuthorDisplayName}")
                                        .Bold().FontSize(9);
                                    row.ConstantItem(140).AlignRight()
                                        .Text($"{n.CreatedAt:yyyy-MM-dd HH:mm}").FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                });
                                noteCol.Item().PaddingTop(4).Text(n.Content).FontSize(10);
                            });
                    }
                }
            });
        }

        //Prescription

        private void ComposePrescriptionSection(ColumnDescriptor col)
        {
            col.Item().Column(section =>
            {
                section.Spacing(5);
                section.Item().PaddingTop(10)
                    .Text("RECEPTA — Przepisane leki").Bold().FontSize(14)
                    .FontColor(Colors.Indigo.Darken3);

                section.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3); //Name
                        columns.RelativeColumn(2); //Dosage
                        columns.RelativeColumn(1); //Quantity
                        columns.RelativeColumn(1); //Unit price
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                            .Text("Lek").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                            .Text("Dawkowanie").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                            .Text("Ilość").Bold().FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                            .Text("Cena jedn.").Bold().FontColor(Colors.White).AlignRight();
                    });

                    if (_medications.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).BorderBottom(1)
                            .BorderColor(Colors.Grey.Lighten3).Padding(5)
                            .Text("Brak przepisanych leków").Italic();
                    }
                    else
                    {
                        foreach (var m in _medications)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(5).Text(m.MedicationName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(5).Text(m.Dosage);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(5).Text(m.Quantity.ToString()).AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(5).Text($"{m.Cost:N2} zł").AlignRight();
                        }
                    }
                });
            });
        }

        //Footer

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Strona ");
                x.CurrentPageNumber();
                x.Span(" z ");
                x.TotalPages();
            });
        }
    }

    private sealed class UpcomingVisitsReportDocument : IDocument
    {
        private readonly IReadOnlyList<VisitDto> _visits;
        private readonly DateOnly _reportDate;

        public UpcomingVisitsReportDocument(IReadOnlyList<VisitDto> visits, DateOnly reportDate)
        {
            _visits = visits;
            _reportDate = reportDate;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text($"RAPORT WIZYT NA {_reportDate:yyyy-MM-dd}")
                        .Bold().FontSize(20).FontColor(Colors.Indigo.Darken3);
                    col.Item().Text($"Wygenerowano: {DateTime.Now:yyyy-MM-dd HH:mm}")
                        .FontSize(9).Italic();
                    col.Item().PaddingTop(5)
                        .Text($"Liczba zaplanowanych wizyt: {_visits.Count}")
                        .FontSize(11);
                });

                page.Content().PaddingVertical(0.8f, Unit.Centimetre).Column(col =>
                {
                    if (_visits.Count == 0)
                    {
                        col.Item().PaddingTop(20).Text("Brak zaplanowanych wizyt.")
                            .Italic().FontSize(12);
                    }
                    else
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);   // Lp.
                                columns.RelativeColumn(2);    // Godzina
                                columns.RelativeColumn(3);    // Pacjent
                                columns.RelativeColumn(3);    // Lekarz
                                columns.RelativeColumn(2);    // Status
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                    .Text("Lp.").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                    .Text("Godzina").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                    .Text("Pacjent").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                    .Text("Lekarz").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Indigo.Darken2).Padding(5)
                                    .Text("Status").Bold().FontColor(Colors.White);
                            });

                            var index = 1;
                            foreach (var v in _visits.OrderBy(v => v.ScheduledAt))
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                    .Padding(5).Text(index.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                    .Padding(5).Text(v.ScheduledAt.ToString("HH:mm"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                    .Padding(5).Text(v.PatientDisplayName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                    .Padding(5).Text(v.DoctorDisplayName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                    .Padding(5).Text(v.Status.ToString());
                                index++;
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Strona ");
                    x.CurrentPageNumber();
                    x.Span(" z ");
                    x.TotalPages();
                });
            });
        }
    }
}
