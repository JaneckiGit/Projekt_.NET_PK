using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models;

public class Patient
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(11)]
    public string Pesel { get; set; } = string.Empty;

    [MaxLength(50)]
    public string InsuranceNumber { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
