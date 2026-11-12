using CmSecurityService.Models;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentType> IncidentTypes => Set<IncidentType>();
    public DbSet<IncidentStatus> IncidentStatuses => Set<IncidentStatus>();

    protected override void OnModelCreating(ModelBuilder b)
    {
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
    }
}