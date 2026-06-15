-- US-15: Analiza Query Plan PRZED dodaniem indeksów
-- Uruchom ten skrypt w SSMS/Azure Data Studio PRZED zastosowaniem migracji
-- US15_AddPerformanceIndexes

USE ClinicManagerDb;
GO

-- Włącz wyświetlanie planu wykonania i statystyk I/O
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO


-- ZAPYTANIE 1: Wyszukiwanie pacjenta po numerze PESEL (StartsWith)
-- Wykorzystywany indeks: IX_Patients_Pesel (unique, filtered)
-- Ten indeks JUŻ istnieje, ale pokażemy plan dla porównania
PRINT '=== ZAPYTANIE 1: Szukaj pacjenta po PESEL ===';
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
-- BRAK indeksu na InsuranceNumber → oczekiwany Table/Clustered Index Scan
PRINT '=== ZAPYTANIE 2: Szukaj pacjenta po InsuranceNumber (BRAK indeksu) ===';
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
-- Indeks IX_Visits_PatientId istnieje ale jest single-column
-- → SQL Server musi wykonać dodatkowe Key Lookup dla ScheduledAt
PRINT '=== ZAPYTANIE 3: Wizyty pacjenta z sortowaniem po dacie (indeks single-column) ===';
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
-- BRAK indeksu na UserId → oczekiwany Table/Clustered Index Scan
PRINT '=== ZAPYTANIE 4: Logi dostępu wg UserId (BRAK indeksu) ===';
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

PRINT '=== KONIEC: Zanotuj plany wykonania (Scan vs Seek) i logical reads ===';
GO
