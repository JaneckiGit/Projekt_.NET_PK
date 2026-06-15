namespace ClinicManager.DTOs;

public class ActiveVisitDto
{
    public int VisitId { get; set; }
    
    public string PatientName { get; set; } = string.Empty;
    
    public string PatientPesel { get; set; } = string.Empty;
    
    public string DoctorName { get; set; } = string.Empty;
    
    public string DoctorSpecialty { get; set; } = string.Empty;
    
    public string Status { get; set; } = string.Empty;
    
    public DateTime ScheduledAt { get; set; }
}
