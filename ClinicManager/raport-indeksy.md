# US-15: Indeksy SQL – Raport analizy wydajności zapytań

## Informacje ogólne

| Pole | Wartość |
|------|---------|
| **User Story** | US-15 |
| **Tytuł** | Indeksy SQL – analiza wydajności zapytań |
| **Autor** | Mateusz Janecki, Paweł Drabik |
| **Data** | 2026-06-04 |
| **Technologia** | ASP.NET Core 10, Entity Framework Core 9.0.5, SQL Server |

---

## 1. Cel

Jako developer chcę dodać indeksy do często filtrowanych kolumn, żeby zapytania były szybsze.

---

## 2. Analiza istniejących indeksów (PRZED zmianą)

Poniższe indeksy istniały już w migracji `InitialCreate`:

| Tabela | Indeks | Kolumny | Typ |
|--------|--------|---------|-----|
| Patients | IX_Patients_Pesel | Pesel | UNIQUE, filtered (IsDeleted=0) |
| Patients | IX_Patients_LastName | LastName | NON-CLUSTERED |
| Visits | IX_Visits_ScheduledAt | ScheduledAt | NON-CLUSTERED |
| Visits | IX_Visits_DoctorId_ScheduledAt | DoctorId, ScheduledAt | NON-CLUSTERED (composite) |
| Visits | IX_Visits_Status | Status | NON-CLUSTERED |
| Visits | IX_Visits_PatientId | PatientId | NON-CLUSTERED (FK) |
| MedicalRecordAccessLogs | IX_MedicalRecordAccessLogs_PatientId | PatientId | NON-CLUSTERED |
| MedicalRecordAccessLogs | IX_MedicalRecordAccessLogs_MedicalRecordId_AccessedAt | MedicalRecordId, AccessedAt | NON-CLUSTERED (composite) |

---

## 3. Zidentyfikowane problemy wydajnościowe

### Problem 1: Brak indeksu na `Patients.InsuranceNumber`
- **Zapytanie**: Wyszukiwanie pacjenta po numerze ubezpieczenia
- **Symptom**: Clustered Index Scan (pełne skanowanie tabeli)
- **Wpływ**: Wysoka liczba logical reads, wolne przy dużej liczbie pacjentów

### Problem 2: Indeks FK `IX_Visits_PatientId` jest single-column
- **Zapytanie**: `SELECT ... FROM Visits WHERE PatientId = @id ORDER BY ScheduledAt DESC`
- **Symptom**: Index Seek + Key Lookup + Sort (dodatkowe operacje)
- **Wpływ**: Konieczność sortowania wyników po ScheduledAt po pobraniu z indeksu

### Problem 3: Brak indeksu na `MedicalRecordAccessLogs.UserId`
- **Zapytanie**: Filtrowanie logów dostępu do kartoteki per użytkownik
- **Symptom**: Clustered Index Scan (pełne skanowanie tabeli logów)
- **Wpływ**: Tabela logów rośnie bardzo szybko – skan jest coraz wolniejszy

---

## 4. Dodane indeksy non-clustered (migracja `US15_AddPerformanceIndexes`)

### Indeks 1: `IX_Patients_InsuranceNumber`
```sql
CREATE NONCLUSTERED INDEX [IX_Patients_InsuranceNumber]
ON [Patients] ([InsuranceNumber]);
```
- **Tabela**: Patients
- **Kolumna**: InsuranceNumber
- **Uzasadnienie**: Wyszukiwanie pacjentów po numerze ubezpieczenia w rejestracji

### Indeks 2: `IX_Visits_PatientId_ScheduledAt` (kompozytowy)
```sql
CREATE NONCLUSTERED INDEX [IX_Visits_PatientId_ScheduledAt]
ON [Visits] ([PatientId], [ScheduledAt]);
```
- **Tabela**: Visits
- **Kolumny**: PatientId, ScheduledAt
- **Uzasadnienie**: Zastępuje indeks single-column `IX_Visits_PatientId` – obsługuje zarówno filtr `WHERE PatientId = @id`, jak i `ORDER BY ScheduledAt DESC` bez dodatkowego sortowania. Klucz FK PatientId jest nadal pokryty.

### Indeks 3: `IX_MedicalRecordAccessLogs_UserId`
```sql
CREATE NONCLUSTERED INDEX [IX_MedicalRecordAccessLogs_UserId]
ON [MedicalRecordAccessLogs] ([UserId]);
```
- **Tabela**: MedicalRecordAccessLogs
- **Kolumna**: UserId
- **Uzasadnienie**: Audyt i przeglądanie logów dostępu do kartotek pacjentów per użytkownik (wymagane przez RODO)

---

## 5. Oczekiwane zmiany w Query Plan

### Zapytanie 2: Wyszukiwanie po InsuranceNumber

| Metryka | PRZED | PO |
|---------|-------|----|
| Operator | Clustered Index Scan | Index Seek (IX_Patients_InsuranceNumber) + Key Lookup |
| Logical Reads | Proporcjonalne do rozmiaru tabeli | Stałe (1-3 strony) |
| Estimated Cost | Wysoki | Niski |

### Zapytanie 3: Historia wizyt pacjenta

| Metryka | PRZED | PO |
|---------|-------|----|
| Operator | Index Seek (IX_Visits_PatientId) + Key Lookup + Sort | Index Seek (IX_Visits_PatientId_ScheduledAt) + Key Lookup |
| Sort | TAK (dodatkowy koszt) | NIE (dane posortowane w indeksie) |
| Logical Reads | Średni + Sort Warnings przy dużych danych | Niski |

### Zapytanie 4: Logi dostępu po UserId

| Metryka | PRZED | PO |
|---------|-------|----|
| Operator | Clustered Index Scan | Index Seek (IX_MedicalRecordAccessLogs_UserId) + Key Lookup |
| Logical Reads | Proporcjonalne do rozmiaru tabeli | Stałe |
| Estimated Cost | Wysoki (rośnie liniowo) | Niski (stały) |

---

## 6. Screenshoty Query Plan

### PRZED (bez nowych indeksów):
<img width="968" height="223" alt="Przed" src="https://github.com/user-attachments/assets/c53c06f6-b1ba-4d4f-a34d-c5f00c29774b" />

### PO (z nowymi indeksami):
<img width="963" height="276" alt="Po" src="https://github.com/user-attachments/assets/c9c9c2b8-65ac-4755-99f1-626a42461586" />


---

## 7. Instrukcja reprodukcji

### Krok 1: Screenshoty PRZED
1. Uruchom bazę danych (Docker): kontener powinien już działać
2. Zastosuj migrację początkową: `dotnet ef database update 20260531144330_InitialCreate`
3. Otwórz SSMS/Azure Data Studio
4. Włącz "Include Actual Execution Plan" (Ctrl+M)
5. Uruchom skrypt `sql/01_query_plan_before_indexes.sql`
6. Zapisz screenshoty planów wykonania

### Krok 2: Zastosuj migrację
```bash
cd ClinicManager/ClinicManager
dotnet ef database update
```

### Krok 3: Screenshoty PO
1. Uruchom skrypt `sql/02_query_plan_after_indexes.sql`
2. Zapisz screenshoty planów wykonania
3. Porównaj operatory (Scan → Seek) i logical reads

---

## 8. Pliki projektu

| Plik | Opis |
|------|------|
| `Data/ApplicationDbContext.cs` | Definicje indeksów w Fluent API (OnModelCreating) |
| `Migrations/..._US15_AddPerformanceIndexes.cs` | Migracja EF Core z nowymi indeksami |
| `sql/01_query_plan_before_indexes.sql` | Skrypt do analizy Query Plan PRZED |
| `sql/02_query_plan_after_indexes.sql` | Skrypt do analizy Query Plan PO |

---

## 9. DOD – Checklist

- [x] Min. 2 indeksy non-clustered (dodano 3: InsuranceNumber, PatientId+ScheduledAt, UserId)
- [ ] Screenshot Query Plan przed i po (do uzupełnienia w SSMS)
- [ ] Raport: raport-indeksy.pdf z opisem i screenshotami (ten dokument – wyeksportuj do PDF)
