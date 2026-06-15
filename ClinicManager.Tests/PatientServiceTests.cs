using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicManager.Tests;

public class PatientServiceTests : SqliteTestBase
{
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        _service = new PatientService(
            Context,
            new PatientMapper(),
            NullLogger<PatientService>.Instance);
    }

    private async Task<Patient> SeedPatientAsync(
        string firstName = "Jan",
        string lastName = "Kowalski",
        string pesel = "90010112345",
        bool isDeleted = false)
    {
        var patient = new Patient
        {
            FirstName = firstName,
            LastName = lastName,
            Pesel = pesel,
            InsuranceNumber = "INS-" + pesel,
            DateOfBirth = new DateOnly(1990, 1, 1),
            IsDeleted = isDeleted,
            DeletedAt = isDeleted ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        Context.Patients.Add(patient);
        await Context.SaveChangesAsync();
        return patient;
    }

    private static PatientFormDto BuildForm(
        string firstName = "Anna",
        string lastName = "Nowak",
        string pesel = "85052298765")
        => new()
        {
            FirstName = firstName,
            LastName = lastName,
            Pesel = pesel,
            InsuranceNumber = "INS-" + pesel,
            DateOfBirth = new DateOnly(1985, 5, 22),
            PhoneNumber = "+48123456789",
            Email = "anna.nowak@example.com",
            Address = "ul. Testowa 1"
        };

    // Bez frazy wyszukiwania serwis zwraca wszystkich pacjentow poza usunietymi (soft delete),
    // posortowanych rosnaco po nazwisku.
    [Fact]
    public async Task SearchAsync_WithoutQuery_ReturnsAllNonDeletedOrderedByLastName()
    {
        await SeedPatientAsync(lastName: "Zielinski", pesel: "11111111111");
        await SeedPatientAsync(lastName: "Adamski", pesel: "22222222222");
        await SeedPatientAsync(lastName: "Usuniety", pesel: "33333333333", isDeleted: true);

        var result = await _service.SearchAsync(null);

        Assert.Equal(2, result.Count);
        Assert.Equal("Adamski", result[0].LastName);
        Assert.Equal("Zielinski", result[1].LastName);
    }

    // Wyszukiwanie po prefiksie nazwiska zwraca tylko pasujacych pacjentow.
    [Fact]
    public async Task SearchAsync_WithLastNamePrefix_ReturnsMatchingPatients()
    {
        await SeedPatientAsync(lastName: "Kowalski", pesel: "11111111111");
        await SeedPatientAsync(lastName: "Nowak", pesel: "22222222222");

        var result = await _service.SearchAsync("Kowal");

        Assert.Single(result);
        Assert.Equal("Kowalski", result[0].LastName);
    }

    // Wyszukiwanie po prefiksie numeru PESEL zwraca tylko pacjenta z pasujacym PESEL-em.
    [Fact]
    public async Task SearchAsync_WithPeselPrefix_ReturnsMatchingPatients()
    {
        await SeedPatientAsync(lastName: "Kowalski", pesel: "90010112345");
        await SeedPatientAsync(lastName: "Nowak", pesel: "85052298765");

        var result = await _service.SearchAsync("9001");

        Assert.Single(result);
        Assert.Equal("90010112345", result[0].Pesel);
    }

    // Dla istniejacego Id serwis zwraca poprawnie zmapowany PatientDto.
    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDto()
    {
        var patient = await SeedPatientAsync();

        var result = await _service.GetByIdAsync(patient.Id);

        Assert.NotNull(result);
        Assert.Equal(patient.Id, result!.Id);
        Assert.Equal("Kowalski", result.LastName);
    }

    // Dla nieistniejacego Id serwis zwraca null (zamiast rzucac wyjatek).
    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // Tworzenie pacjenta zapisuje encje, nadaje Id, ustawia CreatedAt i zwraca zmapowany DTO.
    [Fact]
    public async Task CreateAsync_AddsEntityAndSetsCreatedAt()
    {
        var form = BuildForm();

        var result = await _service.CreateAsync(form);

        Assert.True(result.Id > 0);
        Assert.Equal("Nowak", result.LastName);
        Assert.NotEqual(default, result.CreatedAt);

        var stored = await _service.GetByIdAsync(result.Id);
        Assert.NotNull(stored);
        Assert.Equal("85052298765", stored!.Pesel);
    }

    // Aktualizacja istniejacego pacjenta zmienia pola, ustawia UpdatedAt i zwraca true.
    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesFieldsAndReturnsTrue()
    {
        var patient = await SeedPatientAsync();
        var form = BuildForm(firstName: "Zmienione", lastName: "Nazwisko", pesel: patient.Pesel);

        var updated = await _service.UpdateAsync(patient.Id, form);

        Assert.True(updated);
        var stored = await _service.GetByIdAsync(patient.Id);
        Assert.NotNull(stored);
        Assert.Equal("Zmienione", stored!.FirstName);
        Assert.Equal("Nazwisko", stored.LastName);
        Assert.NotNull(stored.UpdatedAt);
    }

    // Proba aktualizacji nieistniejacego pacjenta zwraca false i niczego nie zmienia.
    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsFalse()
    {
        var updated = await _service.UpdateAsync(999, BuildForm());

        Assert.False(updated);
    }

    // Soft delete oznacza pacjenta jako usunietego, przez co znika z zapytan serwisu
    [Fact]
    public async Task SoftDeleteAsync_WhenExists_HidesPatientFromService()
    {
        var patient = await SeedPatientAsync();

        var deleted = await _service.SoftDeleteAsync(patient.Id);

        Assert.True(deleted);
        var afterDelete = await _service.GetByIdAsync(patient.Id);
        Assert.Null(afterDelete);

        var searchResult = await _service.SearchAsync(null);
        Assert.Empty(searchResult);
    }

    // Proba soft delete nieistniejacego pacjenta zwraca false.
    [Fact]
    public async Task SoftDeleteAsync_WhenMissing_ReturnsFalse()
    {
        var deleted = await _service.SoftDeleteAsync(999);

        Assert.False(deleted);
    }

    // Gdy pacjent o danym PESEL istnieje, kontrola unikalnosci zwraca true.
    [Fact]
    public async Task PeselExistsAsync_WhenPeselPresent_ReturnsTrue()
    {
        await SeedPatientAsync(pesel: "90010112345");

        var exists = await _service.PeselExistsAsync("90010112345");

        Assert.True(exists);
    }

    // Gdy nie ma pacjenta o danym PESEL, kontrola unikalnosci zwraca false.
    [Fact]
    public async Task PeselExistsAsync_WhenPeselAbsent_ReturnsFalse()
    {
        var exists = await _service.PeselExistsAsync("00000000000");

        Assert.False(exists);
    }

    // Gdy jedyny pacjent z danym PESEL jest pominiety przez excludeId (np. przy edycji jego samego),
    // kontrola zwraca false - PESEL nie jest traktowany jako duplikat.
    [Fact]
    public async Task PeselExistsAsync_WhenOnlyMatchIsExcluded_ReturnsFalse()
    {
        var patient = await SeedPatientAsync(pesel: "90010112345");

        var exists = await _service.PeselExistsAsync("90010112345", excludeId: patient.Id);

        Assert.False(exists);
    }
}
