namespace ClinicManager.DTOs;

public class ScanFileDto
{
    public string PhysicalPath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class ScanUploadResultDto
{
    public int PatientId { get; set; }
    public string RelativeUrl { get; set; } = string.Empty;
}
