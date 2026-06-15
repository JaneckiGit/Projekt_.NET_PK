# Raport SQL Profiler – US-16

## Informacje ogólne

| Parametr              | Wartość                                                  |
|-----------------------|----------------------------------------------------------|
| **Projekt**           | ClinicManager (.NET 10, EF Core 9)                       |
| **Endpoint**          | `GET /api/visits/today`                                  |
| **Kontroler**         | `VisitsController.GetTodayVisits()`                      |
| **Serwis**            | `VisitService.GetTodayVisitsAsync()`                     |
| **Metoda profilowania** | EF Core Logging (`EnableSensitiveDataLogging` + `Microsoft.EntityFrameworkCore.Database.Command: Information`) |
| **Data analizy**      | 2026-06-12                                               |
| **Autor**             | Automatycznie wygenerowany                               |

---

## 1. Opis endpointu

### `GET /api/visits/today`

Endpoint zwraca listę wszystkich wizyt zaplanowanych na bieżący dzień (od `00:00:00` do `23:59:59`).

**Atrybuty:**
- `[HttpGet("api/visits/today")]`
- `[AllowAnonymous]` – brak wymagania autoryzacji (endpoint testowy/API)
- `[ProducesResponseType(typeof(IEnumerable<VisitDto>), StatusCodes.Status200OK)]`

**Przepływ danych:**

```
HTTP GET /api/visits/today
    → VisitsController.GetTodayVisits()
        → VisitService.GetTodayVisitsAsync()
            → ApplicationDbContext (EF Core)
                → SQL Server (T-SQL query)
            ← IReadOnlyList<VisitDto>
        ← Ok(visits)
    ← HTTP 200 + JSON
```

---

## 2. Konfiguracja EF Core Logging

### 2.1 Program.cs – DbContext

```csharp
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure());

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();   // ← wyświetla wartości parametrów
        options.EnableDetailedErrors();          // ← szczegóły błędów EF
        options.LogTo(Console.WriteLine, LogLevel.Information); // ← log do konsoli
    }
});
```

### 2.2 appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**Zastosowane opcje logowania:**

| Opcja                        | Cel                                                        |
|------------------------------|------------------------------------------------------------|
| `EnableSensitiveDataLogging` | Wyświetlanie wartości parametrów w logach SQL               |
| `EnableDetailedErrors`       | Szczegółowe informacje o błędach EF Core                   |
| `LogTo(Console.WriteLine)`   | Przekierowanie logów SQL do konsoli aplikacji               |
| Kategoria `Database.Command` | Logi EF Core kategorii `Information` – każde zapytanie SQL  |

---

## 3. Kod źródłowy zapytania (LINQ → EF Core)

### VisitService.GetTodayVisitsAsync()

```csharp
public async Task<IReadOnlyList<VisitDto>> GetTodayVisitsAsync(CancellationToken ct = default)
{
    var todayStart = DateTime.Today;           // np. 2026-06-12 00:00:00
    var todayEnd = todayStart.AddDays(1);      // np. 2026-06-13 00:00:00

    var entities = await _db.Visits
        .AsNoTracking()                         // (1) brak śledzenia zmian
        .Include(v => v.Patient)                // (2) LEFT JOIN → Patients
        .Include(v => v.Doctor)                 // (3) LEFT JOIN → AspNetUsers
        .Where(v => v.ScheduledAt >= todayStart
                  && v.ScheduledAt < todayEnd)  // (4) filtr zakresowy po dacie
        .OrderBy(v => v.ScheduledAt)            // (5) sortowanie rosnące
        .ToListAsync(ct);                       // (6) materializacja

    return entities.Select(_mapper.ToDto).ToList();
}
```

---

## 4. Wygenerowane zapytanie SQL (T-SQL)

Na podstawie analizy LINQ i konfiguracji EF Core, wygenerowane zapytanie SQL ma następującą postać:

```sql
-- EF Core Generated SQL for GET /api/visits/today
-- Kategoria logu: Microsoft.EntityFrameworkCore.Database.Command
-- EventId: RelationalEventId.CommandExecuted (20101)

SELECT
    [v].[Id],
    [v].[CreatedAt],
    [v].[DoctorId],
    [v].[Notes],
    [v].[PatientId],
    [v].[ScheduledAt],
    [v].[Status],
    [v].[UpdatedAt],
    [p].[Id],
    [p].[Address],
    [p].[CreatedAt],
    [p].[DateOfBirth],
    [p].[DeletedAt],
    [p].[Email],
    [p].[FirstName],
    [p].[InsuranceNumber],
    [p].[IsDeleted],
    [p].[LastName],
    [p].[Pesel],
    [p].[PhoneNumber],
    [p].[UpdatedAt],
    [a].[Id],
    [a].[AccessFailedCount],
    [a].[ConcurrencyStamp],
    [a].[Email],
    [a].[EmailConfirmed],
    [a].[FirstName],
    [a].[LastName],
    [a].[LockoutEnabled],
    [a].[LockoutEnd],
    [a].[NormalizedEmail],
    [a].[NormalizedUserName],
    [a].[PasswordHash],
    [a].[PhoneNumber],
    [a].[PhoneNumberConfirmed],
    [a].[SecurityStamp],
    [a].[TwoFactorEnabled],
    [a].[UserName]
FROM [Visits] AS [v]
INNER JOIN [Patients] AS [p] ON [v].[PatientId] = [p].[Id]
LEFT JOIN [AspNetUsers] AS [a] ON [v].[DoctorId] = [a].[Id]
WHERE
    [p].[IsDeleted] = CAST(0 AS bit)            -- Global query filter (soft delete)
    AND [v].[ScheduledAt] >= @__todayStart_0     -- @__todayStart_0 = '2026-06-12T00:00:00'
    AND [v].[ScheduledAt] < @__todayEnd_1        -- @__todayEnd_1 = '2026-06-13T00:00:00'
ORDER BY [v].[ScheduledAt]
```

### Parametry zapytania

| Parametr           | Typ           | Przykładowa wartość            |
|--------------------|---------------|--------------------------------|
| `@__todayStart_0`  | `datetime2(7)` | `2026-06-12T00:00:00.0000000` |
| `@__todayEnd_1`    | `datetime2(7)` | `2026-06-13T00:00:00.0000000` |

---

## 5. Analiza zapytania SQL

### 5.1 Struktura JOINów

| JOIN                | Typ         | Tabela źródłowa | Tabela docelowa  | Klucz                       |
|---------------------|-------------|-----------------|------------------|-----------------------------|
| Patient             | INNER JOIN  | `Visits`        | `Patients`       | `v.PatientId = p.Id`        |
| Doctor              | LEFT JOIN   | `Visits`        | `AspNetUsers`    | `v.DoctorId = a.Id`         |

- **INNER JOIN** z `Patients` – każda wizyta musi mieć przypisanego pacjenta (FK NOT NULL)
- **LEFT JOIN** z `AspNetUsers` – lekarz może być opcjonalny (atrybut `Doctor?` nullable)
- **Global Query Filter**: `[p].[IsDeleted] = CAST(0 AS bit)` – automatyczne wykluczanie soft-deleted pacjentów

### 5.2 Warunki WHERE

| Warunek                                      | Cel                                            |
|-----------------------------------------------|------------------------------------------------|
| `[p].[IsDeleted] = CAST(0 AS bit)`            | Global Query Filter – wyklucza usunięte rekordy |
| `[v].[ScheduledAt] >= @__todayStart_0`        | Dolna granica zakresu dat (włącznie)            |
| `[v].[ScheduledAt] < @__todayEnd_1`           | Górna granica zakresu dat (wyłącznie)           |

**Ważne:** Filtr zakresowy `>= ... AND < ...` jest optymalny dla indeksu – pozwala na **Index Seek** zamiast **Index Scan**.

### 5.3 Wykorzystywane indeksy

Na podstawie konfiguracji w `ApplicationDbContext.OnModelCreating()`:

| Indeks                                       | Kolumny                          | Wykorzystanie                          |
|----------------------------------------------|----------------------------------|----------------------------------------|
| `IX_Visits_ScheduledAt`                       | `ScheduledAt`                    | Seek po zakresie dat + ORDER BY        |
| `IX_Visits_DoctorId_ScheduledAt`              | `DoctorId, ScheduledAt`          | Alternatywny pokrywający               |
| PK `Patients.Id`                              | `Id`                             | INNER JOIN lookup                      |
| PK `AspNetUsers.Id`                           | `Id`                             | LEFT JOIN lookup                       |

### 5.4 Oczekiwany plan wykonania (Execution Plan)

```
|-- Sort (ORDER BY: [v].[ScheduledAt] ASC)
    |-- Nested Loops (Left Outer Join)              ← LEFT JOIN AspNetUsers
        |-- Nested Loops (Inner Join)               ← INNER JOIN Patients
        |   |-- Index Seek (IX_Visits_ScheduledAt)  ← filtr zakresu dat
        |   |-- Clustered Index Seek (PK_Patients)  ← lookup pacjenta po Id
        |-- Clustered Index Seek (PK_AspNetUsers)   ← lookup lekarza po Id
```

**Kluczowe elementy planu:**
- ✅ **Index Seek** na `IX_Visits_ScheduledAt` – optymalny dostęp do danych
- ✅ **Nested Loops Join** – efektywny dla małej liczby wierszy
- ✅ **Clustered Index Seek** na PK tabel powiązanych
- ✅ **Brak Table Scan / Clustered Index Scan**

---

## 6. Aspekty wydajnościowe

### 6.1 Optymalizacje zastosowane w kodzie

| Optymalizacja          | Opis                                                          |
|------------------------|---------------------------------------------------------------|
| `AsNoTracking()`       | Brak śledzenia zmian – mniejsze zużycie pamięci i szybszy odczyt |
| Filtr zakresowy `>= <` | Pozwala na Index Seek zamiast Scan                             |
| Indeks na `ScheduledAt`| Zdefiniowany w `OnModelCreating` – wspiera ten konkretny filtr |
| Parametryzacja         | EF Core automatycznie parametryzuje zapytanie (plan cache)     |
| `CancellationToken`    | Obsługa anulowania – nie blokuje zasobów serwera              |

### 6.2 Potencjalne problemy (N+1)

**Brak problemu N+1** – zapytanie używa `Include()`, co generuje **pojedynczy** SQL z JOINami. Gdyby pominięto `Include()`, EF Core wykonałby:
1. Jeden SELECT na `Visits`
2. N × SELECT na `Patients` (per wizyta)
3. N × SELECT na `AspNetUsers` (per wizyta)

Aktualny kod jest prawidłowy – **1 zapytanie SQL na 1 żądanie HTTP**.

### 6.3 Pobierane kolumny

**Uwaga:** EF Core pobiera **wszystkie kolumny** z trzech tabel (Visits, Patients, AspNetUsers), w tym potencjalnie niepotrzebne jak `PasswordHash`, `SecurityStamp` z `AspNetUsers`. Optymalizacja za pomocą `.Select()` mogłaby zmniejszyć transfer danych, ale dla małej liczby rekordów (wizyty z jednego dnia) wpływ jest minimalny.

---

## 7. Przykładowy log z konsoli aplikacji (EF Core Logging)

Po uruchomieniu aplikacji i wywołaniu `GET /api/visits/today`, w konsoli pojawi się log zbliżony do:

```
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (3ms) [Parameters=[
          @__todayStart_0='2026-06-12T00:00:00.0000000' (Nullable = false),
          @__todayEnd_1='2026-06-13T00:00:00.0000000' (Nullable = false)
      ], CommandType='Text', CommandTimeout='30']

      SELECT [v].[Id], [v].[CreatedAt], [v].[DoctorId], [v].[Notes],
             [v].[PatientId], [v].[ScheduledAt], [v].[Status], [v].[UpdatedAt],
             [p].[Id], [p].[Address], [p].[CreatedAt], [p].[DateOfBirth], ...
             [a].[Id], [a].[FirstName], [a].[LastName], ...
      FROM [Visits] AS [v]
      INNER JOIN [Patients] AS [p] ON [v].[PatientId] = [p].[Id]
      LEFT JOIN [AspNetUsers] AS [a] ON [v].[DoctorId] = [a].[Id]
      WHERE [p].[IsDeleted] = CAST(0 AS bit)
            AND [v].[ScheduledAt] >= @__todayStart_0
            AND [v].[ScheduledAt] < @__todayEnd_1
      ORDER BY [v].[ScheduledAt]
```

**Interpretacja logu:**
- `Executed DbCommand (3ms)` – czas wykonania zapytania w milisekundach
- `[Parameters=...]` – wartości parametrów (widoczne dzięki `EnableSensitiveDataLogging`)
- `CommandType='Text'` – zapytanie ad-hoc (nie procedura składowana)
- `CommandTimeout='30'` – domyślny timeout 30s

---

## 8. Instrukcja weryfikacji

### 8.1 Uruchomienie aplikacji z logowaniem SQL

```bash
# Terminal 1: Upewnij się, że kontener SQL Server działa
docker ps | grep sql

# Terminal 2: Uruchom aplikację
cd ClinicManager/ClinicManager
dotnet run --environment Development
```

### 8.2 Wywołanie endpointu

```bash
# W osobnym terminalu:
curl -s http://localhost:5169/api/visits/today | python3 -m json.tool
```

Alternatywnie w przeglądarce: `http://localhost:5169/swagger` → sekcja `/api/visits/today` → "Try it out" → "Execute"

### 8.3 Weryfikacja logów SQL

Po wywołaniu endpointu, w **konsoli aplikacji** (Terminal 2) powinien pojawić się wpis:

```
info: Microsoft.EntityFrameworkCore.Database.Command[20101]
      Executed DbCommand (Xms) [Parameters=[...], CommandType='Text', ...]
      SELECT ...
      FROM [Visits] AS [v]
      INNER JOIN [Patients] AS [p] ...
      LEFT JOIN [AspNetUsers] AS [a] ...
      WHERE ...
      ORDER BY [v].[ScheduledAt]
```

### 8.4 Weryfikacja planu wykonania w SQL Server

Aby zobaczyć pełny plan wykonania, wykonaj w SSMS lub Azure Data Studio:

```sql
-- Włącz plan wykonania
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Zapytanie analogiczne do wygenerowanego przez EF Core
DECLARE @todayStart datetime2 = CAST(CAST(GETDATE() AS date) AS datetime2);
DECLARE @todayEnd datetime2 = DATEADD(DAY, 1, @todayStart);

SELECT
    [v].[Id], [v].[ScheduledAt], [v].[Status], [v].[Notes],
    [p].[FirstName], [p].[LastName], [p].[Pesel],
    [a].[FirstName] AS DoctorFirstName, [a].[LastName] AS DoctorLastName
FROM [Visits] AS [v]
INNER JOIN [Patients] AS [p] ON [v].[PatientId] = [p].[Id]
LEFT JOIN [AspNetUsers] AS [a] ON [v].[DoctorId] = [a].[Id]
WHERE
    [p].[IsDeleted] = 0
    AND [v].[ScheduledAt] >= @todayStart
    AND [v].[ScheduledAt] < @todayEnd
ORDER BY [v].[ScheduledAt];

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
```

### 8.5 Checklist weryfikacji

| # | Krok                                                     | Oczekiwany wynik                                          | ✓ |
|---|----------------------------------------------------------|-----------------------------------------------------------|---|
| 1 | `dotnet build` kompiluje się bez błędów                  | `Kompilacja powiodła się. Błędy: 0`                      |   |
| 2 | Aplikacja uruchamia się z `dotnet run`                   | Serwer nasłuchuje na `http://localhost:5169`               |   |
| 3 | `GET /api/visits/today` zwraca HTTP 200                  | JSON z listą dzisiejszych wizyt                           |   |
| 4 | W konsoli pojawia się log SQL (EF Core)                  | `Executed DbCommand (Xms)` z pełnym tekstem zapytania     |   |
| 5 | Log zawiera parametry `@__todayStart_0`, `@__todayEnd_1` | Wartości odpowiadające bieżącemu dniu                     |   |
| 6 | Zapytanie zawiera `INNER JOIN [Patients]`                | Poprawna nawigacja do pacjentów                           |   |
| 7 | Zapytanie zawiera `LEFT JOIN [AspNetUsers]`              | Poprawna nawigacja do lekarzy                             |   |
| 8 | Zapytanie zawiera `WHERE [p].[IsDeleted] = 0`            | Global query filter aktywny                               |   |
| 9 | Swagger UI (`/swagger`) pokazuje endpoint                | Endpoint widoczny w dokumentacji API                      |   |
| 10| Brak N+1 problem – tylko 1 zapytanie SQL                 | Jeden `Executed DbCommand` per request                    |   |

---

## 9. Podsumowanie

### Zmiany dokonane w ramach US-16:

1. **Nowy endpoint:** `GET /api/visits/today` w `VisitsController.cs`
2. **Nowa metoda serwisu:** `GetTodayVisitsAsync()` w `VisitService.cs` + `IVisitService.cs`
3. **Konfiguracja EF Core Logging:**
   - `EnableSensitiveDataLogging()` w `Program.cs` (tylko Development)
   - `EnableDetailedErrors()` w `Program.cs` (tylko Development)
   - `LogTo(Console.WriteLine)` w `Program.cs` (tylko Development)
   - Kategoria `Microsoft.EntityFrameworkCore.Database.Command: Information` w `appsettings.Development.json`
4. **Dane testowe:** dodano 2 wizyty na „dziś" w `DataSeeder.cs`

### Pliki zmodyfikowane:

| Plik                               | Zmiana                                          |
|------------------------------------|-------------------------------------------------|
| `Controllers/VisitsController.cs`  | Nowy endpoint `GET /api/visits/today`            |
| `Services/IVisitService.cs`        | Nowa metoda `GetTodayVisitsAsync()`              |
| `Services/VisitService.cs`         | Implementacja `GetTodayVisitsAsync()`            |
| `Program.cs`                       | EF Core SQL Logging (Development only)           |
| `appsettings.Development.json`     | Kategoria logowania `Database.Command`           |
| `Data/DataSeeder.cs`               | Dodatkowe wizyty na dziś                         |

### Wnioski z analizy SQL:

- ✅ Zapytanie jest **parametryzowane** – ochrona przed SQL Injection i cache planu
- ✅ Użyty **filtr zakresowy** (`>= AND <`) – optymalny dla Index Seek
- ✅ `AsNoTracking()` – minimalne zużycie pamięci
- ✅ **Brak problemu N+1** – jedno zapytanie SQL z JOINami
- ✅ **Istniejący indeks** `IX_Visits_ScheduledAt` wspiera ten filtr
- ⚠️ EF Core logowanie **włączone tylko w trybie Development** – brak wpływu na produkcję
