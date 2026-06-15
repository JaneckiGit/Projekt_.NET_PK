namespace ClinicManager.DTOs;

public class PrescribedMedicationDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }

    public int MedicationId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string? MedicationActiveSubstance { get; set; }
    public string? MedicationForm { get; set; }

    public string Dosage { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
