using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using ClinicManager.Models.Enums;
using ClinicManager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ClinicManager.Tests;

public class VisitServiceTests : SqliteTestBase
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly VisitService _service;

    public VisitServiceTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        _service = new VisitService(
            Context,
            new VisitMapper(),
            _userManagerMock.Object,
            NullLogger<VisitService>.Instance);
    }

    private async Task<Patient> SeedPatientAsync(string lastName = "Kowalski", string pesel = "90010112345")
    {
        var patient = new Patient
        {
            FirstName = "Jan",
            LastName = lastName,
            Pesel = pesel,
            InsuranceNumber = "INS-" + pesel,
            DateOfBirth = new DateOnly(1990, 1, 1),
            CreatedAt = DateTime.UtcNow
        };
        Context.Patients.Add(patient);
        await Context.SaveChangesAsync();
        return patient;
    }

    private async Task<ApplicationUser> SeedDoctorAsync(
        string id = "doc-1",
        string firstName = "Jan",
        string lastName = "Lekarski")
    {
        var doctor = new ApplicationUser
        {
            Id = id,
            UserName = $"{firstName}.{lastName}".ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName
        };
        Context.Users.Add(doctor);
        await Context.SaveChangesAsync();
        return doctor;
    }

    private async Task<Visit> SeedVisitAsync(
        int patientId,
        string doctorId,
        DateTime scheduledAt,
        VisitStatus status = VisitStatus.Planned)
    {
        var visit = new Visit
        {
            PatientId = patientId,
            DoctorId = doctorId,
            ScheduledAt = scheduledAt,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        Context.Visits.Add(visit);
        await Context.SaveChangesAsync();
        return visit;
    }

    // Filtr po statusie zwraca tylko wizyty o wybranym statusie.
    [Fact]
    public async Task ListAsync_FiltersByStatus()
    {
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(1), VisitStatus.Planned);
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(2), VisitStatus.Cancelled);

        var result = await _service.ListAsync(new VisitListFilterDto { Status = VisitStatus.Cancelled });

        Assert.Single(result);
        Assert.Equal(VisitStatus.Cancelled, result[0].Status);
    }

    // Filtr po lekarzu zwraca tylko wizyty przypisane do wskazanego DoctorId.
    [Fact]
    public async Task ListAsync_FiltersByDoctor()
    {
        var patient = await SeedPatientAsync();
        var doctor1 = await SeedDoctorAsync(id: "doc-1", lastName: "Pierwszy");
        var doctor2 = await SeedDoctorAsync(id: "doc-2", lastName: "Drugi");
        await SeedVisitAsync(patient.Id, doctor1.Id, DateTime.Today.AddDays(1));
        await SeedVisitAsync(patient.Id, doctor2.Id, DateTime.Today.AddDays(1));

        var result = await _service.ListAsync(new VisitListFilterDto { DoctorId = "doc-2" });

        Assert.Single(result);
        Assert.Equal("doc-2", result[0].DoctorId);
    }

    // Filtr po zakresie dat zwraca tylko wizyty mieszczace sie w zakresie.
    [Fact]
    public async Task ListAsync_FiltersByDateRange()
    {
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        await SeedVisitAsync(patient.Id, doctor.Id, new DateTime(2026, 1, 10, 9, 0, 0));
        await SeedVisitAsync(patient.Id, doctor.Id, new DateTime(2026, 2, 10, 9, 0, 0));

        var filter = new VisitListFilterDto
        {
            DateFrom = new DateOnly(2026, 1, 1),
            DateTo = new DateOnly(2026, 1, 31)
        };

        var result = await _service.ListAsync(filter);

        Assert.Single(result);
        Assert.Equal(new DateTime(2026, 1, 10, 9, 0, 0), result[0].ScheduledAt);
    }

    // Lista aktywnych wizyt zawiera wylacznie wizyty zaplanowane i w trakcie.
    [Fact]
    public async Task GetActiveVisitsAsync_ReturnsOnlyPlannedAndInProgress()
    {
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(1), VisitStatus.Planned);
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(2), VisitStatus.InProgress);
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(3), VisitStatus.Completed);
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(4), VisitStatus.Cancelled);

        var result = (await _service.GetActiveVisitsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, v =>
            Assert.True(v.Status == VisitStatus.Planned || v.Status == VisitStatus.InProgress));
    }

    // Lista dzisiejszych wizyt pomija wizyty z innych dni.
    [Fact]
    public async Task GetTodayVisitsAsync_ReturnsOnlyTodaysVisits()
    {
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddHours(10));
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(1).AddHours(10));
        await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(-1).AddHours(10));

        var result = await _service.GetTodayVisitsAsync();

        Assert.Single(result);
        Assert.Equal(DateTime.Today, result[0].ScheduledAt.Date);
    }

    // Dla istniejacej wizyty serwis zwraca DTO wraz z danymi powiazanego pacjenta i lekarza.
    [Fact]
    public async Task GetByIdAsync_WhenExists_ReturnsDtoWithRelatedData()
    {
        var patient = await SeedPatientAsync(lastName: "Testowy");
        var doctor = await SeedDoctorAsync(lastName: "Medyk");
        var visit = await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(1));

        var result = await _service.GetByIdAsync(visit.Id);

        Assert.NotNull(result);
        Assert.Equal(visit.Id, result!.Id);
        Assert.Equal("Testowy", result.PatientLastName);
        Assert.Equal("Medyk", result.DoctorLastName);
    }

    // Dla nieistniejacej wizyty serwis zwraca null.
    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    // Tworzenie wizyty ustawia status Planned i zwraca DTO z doladowanymi danymi pacjenta i lekarza.
    [Fact]
    public async Task CreateAsync_SetsStatusPlannedAndReturnsRelatedData()
    {
        var patient = await SeedPatientAsync(lastName: "Pacjent");
        var doctor = await SeedDoctorAsync(lastName: "Doktor");

        var form = new VisitFormDto
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ScheduledAt = DateTime.Today.AddDays(3).AddHours(9),
            Notes = "Kontrola"
        };

        var result = await _service.CreateAsync(form);

        Assert.True(result.Id > 0);
        Assert.Equal(VisitStatus.Planned, result.Status);
        Assert.Equal("Pacjent", result.PatientLastName);
        Assert.Equal("Doktor", result.DoctorLastName);
        Assert.Equal("Kontrola", result.Notes);
    }

    // Aktualizacja istniejacej wizyty zmienia pola, ustawia UpdatedAt i zwraca true.
    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturnsTrue()
    {
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var visit = await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(1));

        var form = new VisitFormDto
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            ScheduledAt = DateTime.Today.AddDays(5).AddHours(12),
            Notes = "Zmienione notatki"
        };

        var updated = await _service.UpdateAsync(visit.Id, form);

        Assert.True(updated);
        var stored = await _service.GetByIdAsync(visit.Id);
        Assert.NotNull(stored);
        Assert.Equal("Zmienione notatki", stored!.Notes);
        Assert.NotNull(stored.UpdatedAt);
    }

    // Proba aktualizacji nieistniejacej wizyty zwraca false.
    [Fact]
    public async Task UpdateAsync_WhenMissing_ReturnsFalse()
    {
        var form = new VisitFormDto
        {
            PatientId = 1,
            DoctorId = "doc-1",
            ScheduledAt = DateTime.Today.AddDays(1)
        };

        var updated = await _service.UpdateAsync(999, form);

        Assert.False(updated);
    }

    // Anulowanie istniejacej wizyty ustawia status Cancelled i zwraca true.
    [Fact]
    public async Task CancelAsync_WhenExists_SetsStatusCancelled()
    {
        var patient = await SeedPatientAsync();
        var doctor = await SeedDoctorAsync();
        var visit = await SeedVisitAsync(patient.Id, doctor.Id, DateTime.Today.AddDays(1), VisitStatus.Planned);

        var cancelled = await _service.CancelAsync(visit.Id);

        Assert.True(cancelled);
        var stored = await _service.GetByIdAsync(visit.Id);
        Assert.NotNull(stored);
        Assert.Equal(VisitStatus.Cancelled, stored!.Status);
    }

    // Proba anulowania nieistniejacej wizyty zwraca false.
    [Fact]
    public async Task CancelAsync_WhenMissing_ReturnsFalse()
    {
        var cancelled = await _service.CancelAsync(999);

        Assert.False(cancelled);
    }

    // Kontrola istnienia pacjenta zwraca true dla istniejacego Id i false dla nieistniejacego.
    [Fact]
    public async Task PatientExistsAsync_ReturnsTrueWhenPresentAndFalseWhenAbsent()
    {
        var patient = await SeedPatientAsync();

        Assert.True(await _service.PatientExistsAsync(patient.Id));
        Assert.False(await _service.PatientExistsAsync(999));
    }

    // Lista lekarzy pobierana jest z UserManagera i zwracana posortowana po nazwisku.
    [Fact]
    public async Task GetDoctorsAsync_ReturnsDoctorsFromUserManagerSortedByLastName()
    {
        var doctorB = new ApplicationUser { Id = "doc-b", FirstName = "Adam", LastName = "Zielinski" };
        var doctorA = new ApplicationUser { Id = "doc-a", FirstName = "Ewa", LastName = "Adamska" };

        _userManagerMock
            .Setup(m => m.GetUsersInRoleAsync(Roles.Lekarz))
            .ReturnsAsync(new List<ApplicationUser> { doctorB, doctorA });

        var result = await _service.GetDoctorsAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Adamska", result[0].LastName);
        Assert.Equal("Zielinski", result[1].LastName);
        _userManagerMock.Verify(m => m.GetUsersInRoleAsync(Roles.Lekarz), Times.Once);
    }

    // Gdy uzytkownik istnieje i ma role Lekarz, kontrola DoctorExists zwraca true.
    [Fact]
    public async Task DoctorExistsAsync_WhenUserInDoctorRole_ReturnsTrue()
    {
        var doctor = new ApplicationUser { Id = "doc-1", FirstName = "Jan", LastName = "Lekarski" };
        _userManagerMock.Setup(m => m.FindByIdAsync("doc-1")).ReturnsAsync(doctor);
        _userManagerMock.Setup(m => m.IsInRoleAsync(doctor, Roles.Lekarz)).ReturnsAsync(true);

        var exists = await _service.DoctorExistsAsync("doc-1");

        Assert.True(exists);
    }

    // Gdy uzytkownik o danym Id nie istnieje, kontrola DoctorExists zwraca false.
    [Fact]
    public async Task DoctorExistsAsync_WhenUserNotFound_ReturnsFalse()
    {
        _userManagerMock.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var exists = await _service.DoctorExistsAsync("missing");

        Assert.False(exists);
    }

    // Gdy uzytkownik istnieje, ale nie ma roli Lekarz, kontrola DoctorExists zwraca false.
    [Fact]
    public async Task DoctorExistsAsync_WhenUserNotInDoctorRole_ReturnsFalse()
    {
        var user = new ApplicationUser { Id = "user-1", FirstName = "Anna", LastName = "Recepcja" };
        _userManagerMock.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.IsInRoleAsync(user, Roles.Lekarz)).ReturnsAsync(false);

        var exists = await _service.DoctorExistsAsync("user-1");

        Assert.False(exists);
    }

    // Dla pustego/bialego DoctorId kontrola DoctorExists zwraca false bez odpytywania UserManagera.
    [Fact]
    public async Task DoctorExistsAsync_WhenIdEmpty_ReturnsFalse()
    {
        var exists = await _service.DoctorExistsAsync("  ");

        Assert.False(exists);
    }
}
