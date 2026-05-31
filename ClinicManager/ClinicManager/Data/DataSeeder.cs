using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Models;
using ClinicManager.Models.Enums;

namespace ClinicManager.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
        
        var adminEmail = "admin@przychodnia.pl";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "Systemu"
            };

            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
            else
            {
                throw new Exception($"Failed to seed Admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        
        var doctorEmail = "lekarz@przychodnia.pl";
        var doctorUser = await userManager.FindByEmailAsync(doctorEmail);
        if (doctorUser == null)
        {
            doctorUser = new ApplicationUser
            {
                UserName = doctorEmail,
                Email = doctorEmail,
                EmailConfirmed = true,
                FirstName = "dr Jan",
                LastName = "Kowalski"
            };

            var result = await userManager.CreateAsync(doctorUser, "Lekarz123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(doctorUser, Roles.Lekarz);
            }
            else
            {
                throw new Exception($"Failed to seed Doctor user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        
        var patientsData = new List<(string FirstName, string LastName, string Pesel, string InsuranceNumber, DateOnly DOB)>
        {
            ("Jan", "Nowak", "90010112345", "INS-11111", new DateOnly(1990, 1, 1)),
            ("Anna", "Wiśniewska", "92020254321", "INS-22222", new DateOnly(1992, 2, 2)),
            ("Piotr", "Kowalczyk", "85050598765", "INS-33333", new DateOnly(1985, 5, 5))
        };

        var seededPatients = new List<Patient>();

        foreach (var pData in patientsData)
        {
            var patient = await db.Patients.FirstOrDefaultAsync(p => p.Pesel == pData.Pesel);
            if (patient == null)
            {
                patient = new Patient
                {
                    FirstName = pData.FirstName,
                    LastName = pData.LastName,
                    Pesel = pData.Pesel,
                    InsuranceNumber = pData.InsuranceNumber,
                    DateOfBirth = pData.DOB,
                    PhoneNumber = "123456789",
                    Email = $"{pData.FirstName.ToLower()}.{pData.LastName.ToLower()}@przykladowy.pl",
                    Address = "ul. Medyczna 1, Warszawa",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };
                db.Patients.Add(patient);
                await db.SaveChangesAsync();
            }
            seededPatients.Add(patient);
        }
        
        var medicationsData = new List<(string Name, string ActiveSubstance, string Form, string Dosage, decimal Price)>
        {
            ("Paracetamol Accord", "Paracetamolum", "tabletka", "500mg co 6h", 5.50m),
            ("Ibuprofen Hasco", "Ibuprofenum", "kapsułka", "400mg co 8h", 6.80m),
            ("Amotaks", "Amoxicillinum", "zawiesina doustna", "500mg co 12h", 15.00m)
        };

        foreach (var mData in medicationsData)
        {
            var exists = await db.Medications.AnyAsync(m => m.Name == mData.Name);
            if (!exists)
            {
                db.Medications.Add(new Medication
                {
                    Name = mData.Name,
                    ActiveSubstance = mData.ActiveSubstance,
                    Form = mData.Form,
                    DefaultDosage = mData.Dosage,
                    UnitPrice = mData.Price
                });
            }
        }
        await db.SaveChangesAsync();
        
        var visitsExist = await db.Visits.AnyAsync(v => v.DoctorId == doctorUser.Id);
        if (!visitsExist && seededPatients.Count >= 2)
        {
            db.Visits.Add(new Visit
            {
                ScheduledAt = DateTime.UtcNow.Date.AddDays(1).AddHours(10), // Tomorrow at 10:00
                Status = VisitStatus.Planned,
                PatientId = seededPatients[0].Id,
                DoctorId = doctorUser.Id,
                Notes = "Standardowa wizyta kontrolna po badaniach.",
                CreatedAt = DateTime.UtcNow
            });

            db.Visits.Add(new Visit
            {
                ScheduledAt = DateTime.UtcNow.Date.AddDays(-1).AddHours(14), // Yesterday at 14:00
                Status = VisitStatus.Completed,
                PatientId = seededPatients[1].Id,
                DoctorId = doctorUser.Id,
                Notes = "Konsultacja wyników krwi. Pacjent czuje się dobrze.",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
    }
}
