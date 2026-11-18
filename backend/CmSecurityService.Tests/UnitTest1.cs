using CmSecurityService.Data;
using CmSecurityService.Models;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Tests;

public class UnitTest1
{
    [Fact]
    public void Officer_model_uses_char36_columns_with_mysql_provider()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=localhost;Database=security_db;User=security_app;Password=${MYSQL_PASSWORD};GuidFormat=None;",
                new MySqlServerVersion(new Version(8, 4, 0)))
            .Options;

        using var context = new AppDbContext(options);
        var officer = context.Model.FindEntityType(typeof(Officer));

        Assert.NotNull(officer);
        Assert.Equal("char(36)", officer.FindProperty(nameof(Officer.OfficerId))?.GetColumnType());
        Assert.Equal("char(36)", officer.FindProperty(nameof(Officer.UserId))?.GetColumnType());
    }
}