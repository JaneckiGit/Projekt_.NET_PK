namespace ClinicManager.DTOs;

public class MedicationOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ActiveSubstance { get; set; }
    public string? Form { get; set; }
    public string? DefaultDosage { get; set; }

    public string DisplayName => string.IsNullOrWhiteSpace(ActiveSubstance)
        ? Name
        : $"{Name} ({ActiveSubstance})";
}
