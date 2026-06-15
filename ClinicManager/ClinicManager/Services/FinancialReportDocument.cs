using System;
using System.Collections.Generic;
using ClinicManager.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Services;

public class FinancialReportDocument : IDocument
{
    private readonly FinancialReportViewModel _model;

    public FinancialReportDocument(FinancialReportViewModel model)
    {
        _model = model;
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

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("RAPORT FINANSOWY").Bold().FontSize(22).FontColor(Colors.Indigo.Darken3);
                col.Item().Text($"Wygenerowano: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9).Italic();

                // Add details of applied filters
                var activeFilters = new List<string>();
                if (_model.PatientId.HasValue) activeFilters.Add("Wybrany pacjent");
                if (!string.IsNullOrEmpty(_model.DoctorId)) activeFilters.Add("Wybrany lekarz");
                if (_model.DateFrom.HasValue) activeFilters.Add($"Od: {_model.DateFrom.Value:yyyy-MM-dd}");
                if (_model.DateTo.HasValue) activeFilters.Add($"Do: {_model.DateTo.Value:yyyy-MM-dd}");

                var filtersStr = activeFilters.Count > 0 
                    ? "Filtry: " + string.Join(", ", activeFilters) 
                    : "Filtry: Brak (wszystkie dane)";

                col.Item().Text(filtersStr).FontSize(9).FontColor(Colors.Grey.Darken2);
            });

            row.ConstantItem(120).AlignRight().Column(col =>
            {
                col.Item().Text("ClinicManager").Bold().FontSize(16).FontColor(Colors.Indigo.Medium);
                col.Item().Text("Zarządzanie Kliniką").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(0.8f, Unit.Centimetre).Column(col =>
        {
            col.Spacing(20);

            // 1. Summary Cards Row
            col.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                {
                    c.Item().Text("ŁĄCZNY KOSZT").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{_model.TotalCost:N2} PLN").FontSize(16).Bold().FontColor(Colors.Indigo.Darken3);
                });

                row.ConstantItem(10);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                {
                    c.Item().Text("KOSZT PROCEDUR").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{_model.TotalProceduresCost:N2} PLN").FontSize(16).Bold().FontColor(Colors.Green.Darken3);
                });

                row.ConstantItem(10);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(10).Column(c =>
                {
                    c.Item().Text("KOSZT LEKÓW").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                    c.Item().Text($"{_model.TotalMedicationsCost:N2} PLN").FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                });
            });

            // 2. Summary per Patient
            col.Item().Column(patientCol =>
            {
                patientCol.Spacing(5);
                patientCol.Item().Text("Koszty per Pacjent").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);
                
                patientCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Pacjent").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Procedury").Bold().FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Leki").Bold().FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Suma").Bold().FontColor(Colors.White).AlignRight();
                    });

                    if (_model.GroupedByPatient.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text("Brak danych").Italic();
                    }
                    else
                    {
                        foreach (var g in _model.GroupedByPatient)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(g.GroupKey);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.ProceduresCost:N2} zł").AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.MedicationsCost:N2} zł").AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.TotalCost:N2} zł").Bold().AlignRight();
                        }
                    }
                });
            });

            // 3. Summary per Doctor
            col.Item().Column(doctorCol =>
            {
                doctorCol.Spacing(5);
                doctorCol.Item().Text("Koszty per Lekarz").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);

                doctorCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Lekarz").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Procedury").Bold().FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Leki").Bold().FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Suma").Bold().FontColor(Colors.White).AlignRight();
                    });

                    if (_model.GroupedByDoctor.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text("Brak danych").Italic();
                    }
                    else
                    {
                        foreach (var g in _model.GroupedByDoctor)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(g.GroupKey);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.ProceduresCost:N2} zł").AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.MedicationsCost:N2} zł").AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.TotalCost:N2} zł").Bold().AlignRight();
                        }
                    }
                });
            });

            // 4. Summary per Month
            col.Item().Column(monthCol =>
            {
                monthCol.Spacing(5);
                monthCol.Item().Text("Koszty per Miesiąc").Bold().FontSize(12).FontColor(Colors.Indigo.Darken2);

                monthCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Miesiąc").Bold().FontColor(Colors.White);
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Procedury").Bold().FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Leki").Bold().FontColor(Colors.White).AlignRight();
                        header.Cell().Background(Colors.Indigo.Darken2).Padding(5).Text("Suma").Bold().FontColor(Colors.White).AlignRight();
                    });

                    if (_model.GroupedByMonth.Count == 0)
                    {
                        table.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text("Brak danych").Italic();
                    }
                    else
                    {
                        foreach (var g in _model.GroupedByMonth)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(g.GroupKey);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.ProceduresCost:N2} zł").AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.MedicationsCost:N2} zł").AlignRight();
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text($"{g.TotalCost:N2} zł").Bold().AlignRight();
                        }
                    }
                });
            });
        });
    }

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
