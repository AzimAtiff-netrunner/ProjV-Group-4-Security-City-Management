using CmSecurityService.Common;
using CmSecurityService.Controllers;
using CmSecurityService.Data;
using CmSecurityService.Models;
using CmSecurityService.Suspects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CmSecurityService.Tests;

/// <summary>
/// Tests for story #225, part 2: GET /api/security/v1/neighborhoods/crime-rankings.
/// </summary>
public class CrimeRankingTests
{
    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        // Downtown 4, Eastside 3, Westside 2, Uptown 1 (same as seed_suspects_demo.sql)
        db.Incidents.AddRange(
            NewIncident("Downtown", 1), NewIncident("Downtown", 3), NewIncident("Downtown", 5), NewIncident("Downtown", 9),
            NewIncident("Eastside", 2), NewIncident("Eastside", 7), NewIncident("Eastside", 11),
            NewIncident("Westside", 4), NewIncident("Westside", 8),
            NewIncident("Uptown", 10));
        db.SaveChanges();
        return db;
    }

    private static Incident NewIncident(string location, int day) => new()
    {
        IncidentId = Guid.NewGuid(),
        Title = "Incident",
        Description = "Description",
        Location = location,
        IncidentTypeId = 1,
        IncidentStatusId = 1,
        CreatedAt = new DateTime(2026, 11, day, 12, 0, 0, DateTimeKind.Utc)
    };

    private static NeighborhoodsController CreateController(AppDbContext db) =>
        new(new EfSuspectQueryService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static async Task<IReadOnlyList<NeighborhoodRankingDto>> GetRankings(CrimeRankingQuery query)
    {
        using var db = NewContext();
        var result = await CreateController(db).GetCrimeRankings(query, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<IReadOnlyList<NeighborhoodRankingDto>>(ok.Value);
    }

    // ------------------------------------------------------------ ranking

    [Fact]
    public async Task Rankings_HighestCountFirst()
    {
        var rankings = await GetRankings(new CrimeRankingQuery());

        Assert.Equal(4, rankings.Count);
        Assert.Equal(new[] { "Downtown", "Eastside", "Westside", "Uptown" }, rankings.Select(r => r.Neighborhood));
        Assert.Equal(new[] { 4, 3, 2, 1 }, rankings.Select(r => r.IncidentCount));
        Assert.Equal(new[] { 1, 2, 3, 4 }, rankings.Select(r => r.Rank));
    }

    [Fact]
    public async Task Rankings_Top_LimitsRows()
    {
        var rankings = await GetRankings(new CrimeRankingQuery { Top = 2 });

        Assert.Equal(new[] { "Downtown", "Eastside" }, rankings.Select(r => r.Neighborhood));
    }

    [Fact]
    public async Task Rankings_DateRange_CountsOnlyIncidentsInRange()
    {
        // 5 to 9 Nov: Downtown days 5 and 9, Eastside day 7, Westside day 8
        var rankings = await GetRankings(new CrimeRankingQuery
        {
            From = new DateTime(2026, 11, 5),
            To = new DateTime(2026, 11, 9)
        });

        Assert.Equal(new[] { "Downtown", "Eastside", "Westside" }, rankings.Select(r => r.Neighborhood));
        Assert.Equal(new[] { 2, 1, 1 }, rankings.Select(r => r.IncidentCount));
        Assert.Equal(new[] { 1, 2, 2 }, rankings.Select(r => r.Rank));
    }

    [Fact]
    public async Task Rankings_ToDateWithoutTime_IncludesThatWholeDay()
    {
        // Every seeded incident is at 12:00; "to = 1 Nov" must still include the one on 1 Nov.
        var rankings = await GetRankings(new CrimeRankingQuery { To = new DateTime(2026, 11, 1) });

        var only = Assert.Single(rankings);
        Assert.Equal("Downtown", only.Neighborhood);
        Assert.Equal(1, only.IncidentCount);
    }

    [Fact]
    public async Task Rankings_NoIncidentsInRange_ReturnsEmptyList()
    {
        var rankings = await GetRankings(new CrimeRankingQuery { From = new DateTime(2027, 1, 1) });

        Assert.Empty(rankings);
    }

    [Fact]
    public void Rank_EqualCounts_ShareRankAndSkipNext()
    {
        var rows = EfSuspectQueryService.Rank(
            new[] { ("B", 2), ("A", 5), ("C", 2), ("D", 1) }, top: 10);

        Assert.Equal(new[] { "A", "B", "C", "D" }, rows.Select(r => r.Neighborhood));
        Assert.Equal(new[] { 1, 2, 2, 4 }, rows.Select(r => r.Rank));
    }

    // ------------------------------------------------------------ invalid input

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public async Task Rankings_InvalidTop_Returns400InSharedFormat(int top)
    {
        using var db = NewContext();
        var result = await CreateController(db).GetCrimeRankings(new CrimeRankingQuery { Top = top }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ApiError>(bad.Value);
        Assert.Equal("invalid_query", error.Error);
        Assert.Contains("top", error.Message);
    }

    [Fact]
    public async Task Rankings_FromAfterTo_Returns400()
    {
        using var db = NewContext();
        var query = new CrimeRankingQuery { From = new DateTime(2026, 11, 10), To = new DateTime(2026, 11, 1) };
        var result = await CreateController(db).GetCrimeRankings(query, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ApiError>(bad.Value);
        Assert.Equal("invalid_query", error.Error);
    }

    // ------------------------------------------------------------ registration

    [Fact]
    public void AddSuspectViewing_RegistersEfService()
    {
        var services = new ServiceCollection();
        services.AddSuspectViewing();

        var registration = Assert.Single(services, s => s.ServiceType == typeof(ISuspectQueryService));
        Assert.Equal(typeof(EfSuspectQueryService), registration.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
    }
}
