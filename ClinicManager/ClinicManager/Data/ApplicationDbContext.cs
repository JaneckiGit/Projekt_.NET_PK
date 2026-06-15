using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Models;

namespace ClinicManager.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<MedicalEntry> MedicalEntries => Set<MedicalEntry>();
    public DbSet<MedicalRecordAccessLog> MedicalRecordAccessLogs => Set<MedicalRecordAccessLog>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<PrescribedMedication> PrescribedMedications => Set<PrescribedMedication>();
    public DbSet<ProcedurePerformed> ProceduresPerformed => Set<ProcedurePerformed>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Patient>(entity =>
        {
            entity.HasQueryFilter(p => !p.IsDeleted);

            entity.HasIndex(p => p.Pesel)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.HasIndex(p => p.LastName);

            // US-15: indeks non-clustered na InsuranceNumber – wyszukiwanie pacjentów po numerze ubezpieczenia
            entity.HasIndex(p => p.InsuranceNumber)
                .HasDatabaseName("IX_Patients_InsuranceNumber");
        });

        builder.Entity<MedicalRecord>(entity =>
        {
            entity.HasQueryFilter(r => !r.IsDeleted);

            entity.HasIndex(r => r.PatientId)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            entity.HasOne(r => r.Patient)
                .WithMany()
                .HasForeignKey(r => r.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(r => r.Entries)
                .WithOne(e => e.MedicalRecord)
                .HasForeignKey(e => e.MedicalRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.AccessLogs)
                .WithOne(l => l.MedicalRecord)
                .HasForeignKey(l => l.MedicalRecordId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MedicalEntry>(entity =>
        {
            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.MedicalRecordId, e.EntryDate })
                .IsDescending(false, true);
        });

        builder.Entity<MedicalRecordAccessLog>(entity =>
        {
            entity.HasIndex(l => new { l.MedicalRecordId, l.AccessedAt })
                .IsDescending(false, true);

            entity.HasIndex(l => l.PatientId);

            // US-15: indeks non-clustered na UserId – filtrowanie logów dostępu po użytkowniku
            entity.HasIndex(l => l.UserId)
                .HasDatabaseName("IX_MedicalRecordAccessLogs_UserId");
        });

        builder.Entity<Visit>(entity =>
        {
            entity.HasOne(v => v.Patient)
                .WithMany()
                .HasForeignKey(v => v.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(v => v.Doctor)
                .WithMany()
                .HasForeignKey(v => v.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(v => v.ProceduresPerformed)
                .WithOne(p => p.Visit)
                .HasForeignKey(p => p.VisitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.PrescribedMedications)
                .WithOne(pm => pm.Visit)
                .HasForeignKey(pm => pm.VisitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(v => v.ScheduledAt);
            entity.HasIndex(v => new { v.DoctorId, v.ScheduledAt });
            entity.HasIndex(v => v.Status);

            // US-15: indeks kompozytowy non-clustered na PatientId + ScheduledAt
            // – szybkie wyszukiwanie historii wizyt konkretnego pacjenta z sortowaniem po dacie
            entity.HasIndex(v => new { v.PatientId, v.ScheduledAt })
                .HasDatabaseName("IX_Visits_PatientId_ScheduledAt");
        });

        builder.Entity<Medication>(entity =>
        {
            entity.HasIndex(m => m.Name)
                .IsUnique();
        });

        builder.Entity<Procedure>(entity =>
        {
            entity.HasIndex(p => p.Name)
                .IsUnique();
        });

        builder.Entity<PrescribedMedication>(entity =>
        {
            entity.HasOne(pm => pm.Medication)
                .WithMany(m => m.PrescribedMedications)
                .HasForeignKey(pm => pm.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(pm => pm.VisitId);
        });

        builder.Entity<ProcedurePerformed>(entity =>
        {
            entity.HasOne(p => p.Procedure)
                .WithMany(pr => pr.ProceduresPerformed)
                .HasForeignKey(p => p.ProcedureId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => p.VisitId);
        });

        builder.Entity<ClinicalNote>(entity =>
        {
            entity.HasOne(n => n.Visit)
                .WithMany(v => v.ClinicalNotes)
                .HasForeignKey(n => n.VisitId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Author)
                .WithMany()
                .HasForeignKey(n => n.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(n => n.VisitId);
            entity.HasIndex(n => new { n.VisitId, n.CreatedAt });
        });
    }
}
