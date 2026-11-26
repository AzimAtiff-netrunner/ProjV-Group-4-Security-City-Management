using CmSecurityService.Models;
using CmSecurityService.Vehicles;
using Microsoft.EntityFrameworkCore;
using CmSecurityService.Applications;

namespace CmSecurityService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Officer> Officers => Set<Officer>();

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<OfficerRole> OfficerRoles => Set<OfficerRole>();

    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentType> IncidentTypes => Set<IncidentType>();
    public DbSet<IncidentStatus> IncidentStatuses => Set<IncidentStatus>();

    // Suspect records (#225)
    public DbSet<Suspect> Suspects => Set<Suspect>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleTelemetry> VehicleTelemetryRecords => Set<VehicleTelemetry>();

    public DbSet<PoliceApplication> PoliceApplications => Set<PoliceApplication>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Vehicle>(e =>
        {
            e.ToTable("vehicle");
            e.HasKey(x => x.VehicleId);
            e.Property(x => x.VehicleId).HasColumnName("vehicle_id").HasColumnType("char(36)").ValueGeneratedOnAdd();
            e.Property(x => x.Callsign).HasColumnName("callsign").HasMaxLength(30).IsRequired();
            e.Property(x => x.LicensePlate).HasColumnName("license_plate").HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(30)
                .HasConversion<string>().IsRequired();
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasPrecision(6)
                .ValueGeneratedOnAdd();
            e.HasIndex(x => x.Callsign).IsUnique().HasDatabaseName("uq_vehicle_callsign");
            e.HasIndex(x => x.LicensePlate).IsUnique().HasDatabaseName("uq_vehicle_license_plate");
        });

        b.Entity<VehicleTelemetry>(e =>
        {
            e.ToTable("vehicle_telemetry");
            e.HasKey(x => x.TelemetryId);
            e.Property(x => x.TelemetryId).HasColumnName("telemetry_id").ValueGeneratedOnAdd();
            e.Property(x => x.VehicleId).HasColumnName("vehicle_id").HasColumnType("char(36)").IsRequired();
            e.Property(x => x.StreetNumber).HasColumnName("street_number").HasMaxLength(10).IsRequired();
            e.Property(x => x.StreetName).HasColumnName("street_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.City).HasColumnName("city").HasMaxLength(50).IsRequired();
            e.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(7).IsRequired();
            e.Property(x => x.SpeedKmh).HasColumnName("speed_kmh").HasPrecision(5, 1);
            e.Property(x => x.HeadingDegrees).HasColumnName("heading_degrees").HasPrecision(5, 2);
            e.Property(x => x.RecordedAt).HasColumnName("recorded_at").HasPrecision(6);
            e.Property(x => x.ReceivedAt).HasColumnName("received_at").HasPrecision(6)
                .ValueGeneratedOnAdd();
            e.HasOne(x => x.Vehicle).WithMany(v => v.Telemetry)
                .HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            // Latest-record lookup per vehicle; also rejects duplicate (vehicle, timestamp) updates.
            e.HasIndex(x => new { x.VehicleId, x.RecordedAt })
                .IsUnique()
                .IsDescending(false, true)
                .HasDatabaseName("uq_telemetry_vehicle_recorded");
        });

        b.Entity<IncidentType>(e =>
        {
            e.ToTable("incident_type");
            e.HasKey(x => x.IncidentTypeId);
            e.Property(x => x.IncidentTypeId).HasColumnName("incident_type_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<IncidentStatus>(e =>
        {
            e.ToTable("incident_status");
            e.HasKey(x => x.IncidentStatusId);
            e.Property(x => x.IncidentStatusId).HasColumnName("incident_status_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<Incident>(e =>
        {
            e.ToTable("incident");
            e.HasKey(x => x.IncidentId);
            e.Property(x => x.IncidentId).HasColumnName("incident_id").ValueGeneratedOnAdd();
            e.Property(x => x.IncidentTypeId).HasColumnName("incident_type_id");
            e.Property(x => x.IncidentStatusId).HasColumnName("incident_status_id");
            e.Property(x => x.Title).HasColumnName("incident_title").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").IsRequired();
            e.Property(x => x.Location).HasColumnName("incident_location").HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasPrecision(6)
                .ValueGeneratedOnAdd();
            e.HasOne(x => x.IncidentType).WithMany()
                .HasForeignKey(x => x.IncidentTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.IncidentStatus).WithMany()
                .HasForeignKey(x => x.IncidentStatusId).OnDelete(DeleteBehavior.Restrict);
        });

        // Suspect records (#225): maps the existing suspect table in security_db.sql.
        b.Entity<Suspect>(e =>
        {
            e.ToTable("suspect");
            e.HasKey(x => x.SuspectId);
            e.Property(x => x.SuspectId).HasColumnName("suspect_id").HasColumnType("char(36)").ValueGeneratedNever();
            e.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
            e.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);
            e.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
            e.Property(x => x.PhysicalDescription).HasColumnName("physical_description");
            e.Property(x => x.KnownAliases).HasColumnName("known_aliases").HasMaxLength(500);
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasPrecision(6);
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("role");
            e.HasKey(x => x.RoleId);

            e.Property(x => x.RoleId).HasColumnName("role_id").ValueGeneratedOnAdd();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<OfficerRole>(e =>
        {
            e.ToTable("officer_role");

            e.HasKey(x => x.OfficerRoleId);

            e.Property(x => x.OfficerRoleId).HasColumnName("officer_role_id").HasColumnType("char(36)").ValueGeneratedNever();

            e.Property(x => x.OfficerId).HasColumnName("officer_id").HasColumnType("char(36)").IsRequired();

            e.Property(x => x.RoleId).HasColumnName("role_id").IsRequired();

            e.Property(x => x.AssignedAt).HasColumnName("assigned_at").HasPrecision(6).HasDefaultValueSql("CURRENT_TIMESTAMP(6)")
                .ValueGeneratedOnAdd();

            e.Property(x => x.RevokedAt).HasColumnName("revoked_at").HasPrecision(6);

            e.HasOne<Officer>().WithMany().HasForeignKey(x => x.OfficerId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.OfficerId).HasDatabaseName("ix_officer_role_officer");

            e.HasIndex(x => x.RoleId).HasDatabaseName("ix_officer_role_role");
        });

        b.Entity<PoliceApplication>(e =>
        {
            e.ToTable("police_application");
            e.HasKey(x => x.ApplicationId);
            e.Property(x => x.ApplicationId).HasColumnName("application_id").HasColumnType("char(36)").ValueGeneratedOnAdd();
            e.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("char(36)");
            e.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
            e.Property(x => x.DateOfBirth).HasColumnName("date_of_birth").HasColumnType("date").IsRequired();
            e.Property(x => x.Address).HasColumnName("address").HasMaxLength(255).IsRequired();
            e.Property(x => x.Qualifications).HasColumnName("qualifications");
            e.Property(x => x.ExperienceYears).HasColumnName("experience_years").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasMaxLength(20)
                .HasConversion<string>().IsRequired();
            e.Property(x => x.ReviewerNotes).HasColumnName("reviewer_notes").HasMaxLength(500);
            e.Property(x => x.ReviewedByOfficerId).HasColumnName("reviewed_by_officer_id").HasColumnType("char(36)");
            e.Property(x => x.SubmittedAt).HasColumnName("submitted_at").HasPrecision(6).ValueGeneratedOnAdd();
            e.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasPrecision(6);
            e.HasOne(x => x.ReviewedByOfficer).WithMany()
                .HasForeignKey(x => x.ReviewedByOfficerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Status).HasDatabaseName("ix_police_application_status");
            e.HasIndex(x => x.SubmittedAt).HasDatabaseName("ix_police_application_submitted_at");
        });
    }
}