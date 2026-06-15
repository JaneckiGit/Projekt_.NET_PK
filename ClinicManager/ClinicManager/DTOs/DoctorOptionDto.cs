namespace ClinicManager.DTOs;

public class DoctorOptionDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string DisplayName => $"{LastName} {FirstName}".Trim();
}
