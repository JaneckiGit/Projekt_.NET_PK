namespace ClinicManager.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Lekarz = "Lekarz";
    public const string Rejestratorka = "Rejestratorka";

    public static readonly string[] All = { Admin, Lekarz, Rejestratorka };
}
