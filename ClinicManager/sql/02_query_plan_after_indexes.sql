-- US-15: Analiza Query Plan PO dodaniu indeksów
-- Uruchom ten skrypt w SSMS/Azure Data Studio PO zastosowaniu migracji
-- US15_AddPerformanceIndexes (dotnet ef database update)

USE ClinicManagerDb;
GO

-- Włącz wyświetlanie statystyk I/O
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO

-- ZAPYTANIE 1: Wyszukiwanie pacjenta po numerze PESEL (bez zmian – baseline)
-- Indeks IX_Patients_Pesel (unique, filtered) – pozostaje bez zmian
PRINT '=== ZAPYTANIE 1: Szukaj pacjenta po PESEL (baseline) ===';
GO

SET SHOWPLAN_XML ON;
GO
SELECT Id, FirstName, LastName, Pesel, InsuranceNumber
FROM Patients
WHERE IsDeleted = 0 AND Pesel LIKE '90010%';
GO
SET SHOWPLAN_XML OFF;
GO

-- ZAPYTANIE 2: Wyszukiwanie pacjenta po numerze ubezpieczenia (InsuranceNumber)
-- NOWY indeks: IX_Patients_InsuranceNumber → oczekiwany Index Seek
PRINT '=== ZAPYTANIE 2: Szukaj pacjenta po InsuranceNumber (NOWY indeks) ===';
GO

SET SHOWPLAN_XML ON;
GO
SELECT Id, FirstName, LastName, Pesel, InsuranceNumber
FROM Patients
WHERE IsDeleted = 0 AND InsuranceNumber = 'INS-001';
GO
SET SHOWPLAN_XML OFF;
GO

-- ZAPYTANIE 3: Historia wizyt pacjenta z sortowaniem po dacie
-- NOWY indeks kompozytowy: IX_Visits_PatientId_ScheduledAt
-- → oczekiwany Index Seek (bez Key Lookup i Sort)
PRINT '=== ZAPYTANIE 3: Wizyty pacjenta z sortowaniem (NOWY indeks kompozytowy) ===';
GO

SET SHOWPLAN_XML ON;
GO
SELECT v.Id, v.ScheduledAt, v.Status, v.DoctorId
FROM Visits v
WHERE v.PatientId = 1
ORDER BY v.ScheduledAt DESC;
GO
SET SHOWPLAN_XML OFF;
GO
-- ZAPYTANIE 4: Logi dostępu do kartoteki po UserId
-- NOWY indeks: IX_MedicalRecordAccessLogs_UserId → oczekiwany Index Seek
PRINT '=== ZAPYTANIE 4: Logi dostępu wg UserId (NOWY indeks) ===';
GO

SET SHOWPLAN_XML ON;
GO
SELECT Id, MedicalRecordId, PatientId, UserName, Action, AccessedAt
FROM MedicalRecordAccessLogs
WHERE UserId = 'some-user-guid-here'
ORDER BY AccessedAt DESC;
GO
SET SHOWPLAN_XML OFF;
GO

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO

-- WERYFIKACJA: Sprawdzenie, że nowe indeksy istnieją w bazie danych
PRINT '=== WERYFIKACJA: Lista indeksów non-clustered ===';
GO

SELECT
    t.name AS [Tabela],
    i.name AS [Indeks],
    i.type_desc AS [Typ],
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS [Kolumny]
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.type_desc = 'NONCLUSTERED'
  AND t.name IN ('Patients', 'Visits', 'MedicalRecordAccessLogs')
  AND i.name IN (
      'IX_Patients_InsuranceNumber',
      'IX_Visits_PatientId_ScheduledAt',
      'IX_MedicalRecordAccessLogs_UserId',
      'IX_Patients_Pesel',
      'IX_Patients_LastName',
      'IX_Visits_DoctorId_ScheduledAt',
      'IX_Visits_ScheduledAt',
      'IX_Visits_Status',
      'IX_Visits_PatientId'
  )
GROUP BY t.name, i.name, i.type_desc
ORDER BY t.name, i.name;
GO

PRINT '=== KONIEC: Porównaj plany z 01_query_plan_before_indexes.sql ===';
PRINT '=== Oczekiwana poprawa: Scan → Seek, mniejsza liczba logical reads ===';
GO
