using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class NotInFutureAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null) return ValidationResult.Success;

        var today = DateOnly.FromDateTime(DateTime.Today);

        DateOnly date = value switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            _ => today
        };

        if (date > today)
        {
            return new ValidationResult(
                ErrorMessage ?? $"{validationContext.DisplayName} nie moze byc z przyszlosci.",
                new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }
}
