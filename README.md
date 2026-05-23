# ClinicManager 

System zarządzania przychodnią medyczną – projekt zaliczeniowy ASP.NET Core 10.

## Zespół
- Mateusz Janecki (155182)
- Paweł Drabik (155171)

## Technologie
- ASP.NET Core 10 MVC
- Entity Framework Core (Code First, SQL Server)
- ASP.NET Identity (role: Admin, Lekarz, Rejestratorka)
- Mapperly (mapowanie DTO ↔ encje)
- NLog (logowanie błędów do pliku)
- QuestPDF (generowanie raportów PDF)

## Uruchomienie
1. Sklonuj repo i przejdź na branch `dev`
2. Ustaw connection string w `appsettings.json`
3. Uruchom migracje: `dotnet ef database update`
4. Uruchom projekt: `dotnet run`

## Struktura projektu
```
/ClinicManager
├── Controllers/
├── DTOs/
├── Models/
├── Services/
├── Mappers/
├── Views/
├── wwwroot/uploads/
├── Data/
├── Program.cs
```
## CI/CD (GitHub Actions)
Projekt wykorzystuje GitHub Actions do automatyzacji procesów (CI/CD)
Konfiguracja znajduje się w pliku `.github/workflows/dotnet-ci.yml`.

Główne etapy pipeline'u uruchamiane przy każdym *pushu* lub *pull requeście*:
1. **Build** (`dotnet build`) – sprawdzanie, czy projekt kompiluje się bez błędów.
2. **Test** (`dotnet test`) – automatyczne wykonywanie testów jednostkowych i integracyjnych.

## Dane logowania

| Rola         | E-mail                        | Hasło        |
|--------------|-------------------------------|--------------|
| Admin        | admin@klinika.pl               | Admin123!    |
| Lekarz       | lekarz@klinika.pl              | Lekarz123!   |
| Rejestratorka| rejestratorka@klinika.pl       | Rejest123!   |
