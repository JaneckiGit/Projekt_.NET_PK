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
        });
    }
}
