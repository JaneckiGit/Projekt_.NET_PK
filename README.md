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

## Role i uprawnienia

System definiuje trzy role (`Models/Roles.cs`). Każda strona wymaga zalogowania (globalna polityka `FallbackPolicy`).

### Admin
Pełen dostęp do całego systemu.

| Moduł | Uprawnienia |
|---|---|
| Panel administracyjny | Dashboard, raport finansowy (HTML + PDF) |
| Pacjenci | Przeglądanie, dodawanie, edycja, usuwanie (soft delete), upload skanów |
| Kartoteka medyczna | Przeglądanie, edycja podsumowania, wpisy (CRUD), usuwanie kartoteki (soft delete), upload skanów, **log dostępu** (wyłącznie Admin) |
| Wizyty | Przeglądanie listy i szczegółów, tworzenie, edycja, anulowanie, zmiana statusu, pobieranie PDF |
| Procedury przy wizycie | Dodawanie, edycja, usuwanie procedur wykonanych i leków przepisanych |
| Notatki kliniczne | Dodawanie, edycja, usuwanie |
| Katalog procedur | CRUD (wyłącznie Admin) |
| Katalog leków | Przeglądanie, dodawanie, edycja, usuwanie |

### Lekarz
Dostęp do danych medycznych pacjentów i zarządzania wizytami.

| Moduł | Uprawnienia |
|---|---|
| Panel lekarza | Dashboard |
| Pacjenci | Przeglądanie listy i szczegółów (bez tworzenia/edycji/usuwania) |
| Kartoteka medyczna | Przeglądanie, edycja podsumowania, wpisy (CRUD), usuwanie kartoteki (soft delete), upload skanów |
| Wizyty | Przeglądanie listy i szczegółów, zmiana statusu, pobieranie PDF (bez tworzenia/edycji/anulowania) |
| Procedury przy wizycie | Dodawanie, edycja, usuwanie procedur wykonanych i leków przepisanych |
| Notatki kliniczne | Dodawanie, edycja, usuwanie |

### Rejestratorka
Zarządzanie pacjentami i planowanie wizyt — bez dostępu do danych medycznych.

| Moduł | Uprawnienia |
|---|---|
| Panel recepcji | Dashboard |
| Pacjenci | Przeglądanie, dodawanie, edycja, usuwanie (soft delete), upload skanów |
| Wizyty | Przeglądanie listy i szczegółów, tworzenie, edycja, anulowanie, zmiana statusu (bez PDF) |
| Procedury przy wizycie | Dodawanie, edycja, usuwanie procedur wykonanych i leków przepisanych |
| Katalog leków | Przeglądanie, dodawanie, edycja, usuwanie |
| Kartoteka medyczna | Brak dostępu |
| Notatki kliniczne | Brak dostępu |
| Katalog procedur | Brak dostępu |
| Raport finansowy | Brak dostępu |

## Dane logowania

| Rola         | E-mail                        | Hasło        |
|--------------|-------------------------------|--------------|
| Admin        | admin@klinika.pl               | Admin123!    |
| Lekarz       | lekarz@klinika.pl              | Lekarz123!   |
| Rejestratorka| rejestratorka@klinika.pl       | Rejest123!   |
