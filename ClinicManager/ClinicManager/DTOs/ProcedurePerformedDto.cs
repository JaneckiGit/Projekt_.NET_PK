namespace ClinicManager.DTOs;

public class ProcedurePerformedDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Cost { get; set; }
    public DateTime CreatedAt { get; set; }
}
