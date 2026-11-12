using CmSecurityService.Models;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
    {
        
    }

    public DbSet<Officer> Officers { get; set; } = null!;
}