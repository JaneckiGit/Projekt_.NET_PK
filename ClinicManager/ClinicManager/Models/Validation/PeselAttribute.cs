using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models.Validation;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class PeselAttribute : ValidationAttribute
{
    private static readonly int[] Weights = { 1, 3, 7, 9, 1, 3, 7, 9, 1, 3 };

    public PeselAttribute() : base("Niepoprawny numer PESEL.")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null) return true; // [Required] osobno

        if (value is not string pesel) return false;
        if (pesel.Length != 11) return false;

        var digits = new int[11];
        for (var i = 0; i < 11; i++)
        {
            if (!char.IsDigit(pesel[i])) return false;
            digits[i] = pesel[i] - '0';
        }

        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            sum += digits[i] * Weights[i];
        }

        var checksum = (10 - (sum % 10)) % 10;
        return checksum == digits[10];
    }
}
