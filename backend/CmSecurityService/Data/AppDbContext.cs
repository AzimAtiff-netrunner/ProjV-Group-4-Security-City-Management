using CmSecurityService.Models;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
    {
        
    }

    public DbSet<Officer> Officers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Officer>(entity =>
        {
            entity.ToTable("officer");

            entity.HasKey(o => o.OfficerId);

            entity.Property(o => o.OfficerId)
                .HasColumnName("officer_id")
                .HasColumnType("char(36)")
                .HasDefaultValueSql("UUID()");

            entity.Property(o => o.UserId)
                .HasColumnName("user_id")
                .HasColumnType("char(36)");

            entity.Property(o => o.BadgeNumber)
                .HasColumnName("badge_number")
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(o => o.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(o => o.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(o => o.RankTitle)
                .HasColumnName("rank_title")
                .HasMaxLength(100);

            entity.Property(o => o.Phone)
                .HasColumnName("phone")
                .HasMaxLength(30);

            entity.Property(o => o.Email)
                .HasColumnName("email")
                .HasMaxLength(255);

            // entity.Property(o => o.HireDate)
            //     .HasColumnName("hire_date")
            //     .HasColumnType("date");

            entity.Property(o => o.IsActive)
                .HasColumnName("is_active")
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true);

            entity.Property(o => o.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });
    }
}