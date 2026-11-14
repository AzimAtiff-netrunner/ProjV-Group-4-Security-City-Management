using System.ComponentModel.DataAnnotations;
using CmSecurityService.Data;
using CmSecurityService.Incidents;
using CmSecurityService.Models;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Tests;

/// <summary>
/// Tests for the database-backed incident service, using EF Core's in-memory provider.
/// </summary>
/// <remarks>
/// The in-memory provider does not enforce foreign keys, unique indexes or CHECK constraints,
/// and its text comparison is case-sensitive (MySQL's is not). These tests cover the service's
/// own logic: lookups, rejections, filtering, paging and sorting. The database rules are
/// covered by the manual checks in the README (or integration tests against real MySQL).
/// </remarks>
public class EfIncidentQueryServiceTests
{
    private const uint Theft = 1;
    private const uint Vandalism = 2;
    private const uint Open = 1;
    private const uint UnderInvestigation = 2;
    private const uint Closed = 3;

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.IncidentTypes.AddRange(
            new IncidentType { IncidentTypeId = Theft, Name = "Theft" },
            new IncidentType { IncidentTypeId = Vandalism, Name = "Vandalism" });
        db.IncidentStatuses.AddRange(
            new CmSecurityService.Models.IncidentStatus { IncidentStatusId = Open, Name = "Open" },
            new CmSecurityService.Models.IncidentStatus { IncidentStatusId = UnderInvestigation, Name = "Under Investigation" },
            new CmSecurityService.Models.IncidentStatus { IncidentStatusId = Closed, Name = "Closed" });
        db.SaveChanges();
        return db;
    }

    private static Incident NewIncident(
        string title, uint typeId, uint statusId, DateTime createdAt, string location = "Downtown") => new()
    {
        IncidentId = Guid.NewGuid(),
        Title = title,
        Description = "A description long enough to be valid.",
        Location = location,
        IncidentTypeId = typeId,
        IncidentStatusId = statusId,
        CreatedAt = createdAt
    };

    private static DateTime Utc(int month, int day, int hour = 12, int minute = 0) =>
        new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

    // ---------------------------------------------------------------- AddIncidentAsync

    [Fact]
    public async Task Add_ValidIncident_IsSavedAndReturned()
    {
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);

        var created = await service.AddIncidentAsync(
            "Bike stolen", "Bike taken from the library rack.", "Theft", "Closed", "Downtown");

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Bike stolen", created.Title);
        Assert.Equal("Theft", created.IncidentType);
        Assert.Equal("Closed", created.Status);
        Assert.Equal("Downtown", created.Location);

        var saved = await db.Incidents.SingleAsync();
        Assert.Equal(created.Id, saved.IncidentId);
        Assert.Equal(Theft, saved.IncidentTypeId);
        Assert.Equal(Closed, saved.IncidentStatusId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Add_BlankStatus_DefaultsToOpen(string? status)
    {
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);

        var created = await service.AddIncidentAsync(
            "Bike stolen", "Bike taken from the library rack.", "Theft", status, "Downtown");

        Assert.Equal("Open", created.Status);
        Assert.Equal(Open, (await db.Incidents.SingleAsync()).IncidentStatusId);
    }

    [Fact]
    public async Task Add_TrimsTextBeforeSaving()
    {
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);

        await service.AddIncidentAsync(
            "  Bike stolen  ", "  Bike taken from the library rack.  ", "  Theft  ", "  Open  ", "  Downtown  ");

        var saved = await db.Incidents.SingleAsync();
        Assert.Equal("Bike stolen", saved.Title);
        Assert.Equal("Bike taken from the library rack.", saved.Description);
        Assert.Equal("Downtown", saved.Location);
    }

    [Fact]
    public async Task Add_SetsCreatedAtToCurrentUtcTime()
    {
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);
        var before = DateTime.UtcNow;

        var created = await service.AddIncidentAsync(
            "Bike stolen", "Bike taken from the library rack.", "Theft", null, "Downtown");

        var after = DateTime.UtcNow;
        Assert.InRange(created.ReportedAt, before, after);
    }

    [Fact]
    public async Task Add_UnknownIncidentType_IsRejectedAndNothingIsSaved()
    {
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.AddIncidentAsync(
            "Bike stolen", "Bike taken from the library rack.", "Banana", null, "Downtown"));

        Assert.Contains("Banana", ex.Message);
        Assert.Empty(db.Incidents);
    }

    [Fact]
    public async Task Add_UnknownStatus_IsRejectedAndNothingIsSaved()
    {
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.AddIncidentAsync(
            "Bike stolen", "Bike taken from the library rack.", "Theft", "Banana", "Downtown"));

        Assert.Contains("Banana", ex.Message);
        Assert.Empty(db.Incidents);
    }

    [Fact]
    public async Task Add_SameContentTwice_CreatesTwoSeparateIncidents()
    {
        // Incidents have no uniqueness rule: two reports can look identical.
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);

        var first = await service.AddIncidentAsync(
            "Bike stolen", "Bike taken from the library rack.", "Theft", null, "Downtown");
        var second = await service.AddIncidentAsync(
            "Bike stolen", "Bike taken from the library rack.", "Theft", null, "Downtown");

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, await db.Incidents.CountAsync());
    }

    // ------------------------------------------------------------ GetIncidentByIdAsync

    [Fact]
    public async Task GetById_ExistingIncident_ReturnsDtoWithNames()
    {
        using var db = NewContext();
        var incident = NewIncident("Graffiti", Vandalism, UnderInvestigation, Utc(10, 15), "Uptown");
        db.Incidents.Add(incident);
        await db.SaveChangesAsync();
        var service = new EfIncidentQueryService(db);

        var dto = await service.GetIncidentByIdAsync(incident.IncidentId);

        Assert.NotNull(dto);
        Assert.Equal(incident.IncidentId, dto!.Id);
        Assert.Equal("Graffiti", dto.Title);
        Assert.Equal("Vandalism", dto.IncidentType);
        Assert.Equal("Under Investigation", dto.Status);
        Assert.Equal("Uptown", dto.Location);
        Assert.Equal(incident.CreatedAt, dto.ReportedAt);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNull()
    {
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);

        Assert.Null(await service.GetIncidentByIdAsync(Guid.NewGuid()));
    }

    // --------------------------------------------------------------- GetIncidentsAsync

    private static async Task<EfIncidentQueryService> ServiceWithThreeIncidents(AppDbContext db)
    {
        db.Incidents.AddRange(
            NewIncident("Bike stolen", Theft, Open, Utc(10, 14, 9, 30)),
            NewIncident("Graffiti", Vandalism, Closed, Utc(10, 15, 14, 5)),
            NewIncident("Wallet taken", Theft, UnderInvestigation, Utc(10, 16, 6, 45)));
        await db.SaveChangesAsync();
        return new EfIncidentQueryService(db);
    }

    [Fact]
    public async Task List_NoFilters_ReturnsAllNewestFirst()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var result = await service.GetIncidentsAsync(null, null, null, null, 1, 20);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new[] { "Wallet taken", "Graffiti", "Bike stolen" },
            result.Items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public async Task List_EmptyDatabase_ReturnsEmptyPage()
    {
        using var db = NewContext();
        var service = new EfIncidentQueryService(db);

        var result = await service.GetIncidentsAsync(null, null, null, null, 1, 20);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task List_FilterByStatus_ReturnsOnlyMatches()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var result = await service.GetIncidentsAsync("Closed", null, null, null, 1, 20);

        var only = Assert.Single(result.Items);
        Assert.Equal("Graffiti", only.Title);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task List_UnknownStatus_IsRejected()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetIncidentsAsync("Banana", null, null, null, 1, 20));

        Assert.Contains("Banana", ex.Message);
        Assert.Contains("Open", ex.Message);   // the message lists the valid names
    }

    [Fact]
    public async Task List_FilterByType_ReturnsOnlyMatches()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var result = await service.GetIncidentsAsync(null, "Theft", null, null, 1, 20);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, i => Assert.Equal("Theft", i.IncidentType));
    }

    [Fact]
    public async Task List_FilterByStatusAndType_CombinesFilters()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var result = await service.GetIncidentsAsync("Under Investigation", "Theft", null, null, 1, 20);

        var only = Assert.Single(result.Items);
        Assert.Equal("Wallet taken", only.Title);
    }

    [Fact]
    public async Task List_FromDate_IncludesThatMoment()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var result = await service.GetIncidentsAsync(null, null, Utc(10, 15, 14, 5), null, 1, 20);

        Assert.Equal(new[] { "Wallet taken", "Graffiti" }, result.Items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public async Task List_ToDateWithNoTime_IncludesTheWholeDay()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);
        var oct15 = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = await service.GetIncidentsAsync(null, null, oct15, oct15, 1, 20);

        var only = Assert.Single(result.Items);
        Assert.Equal("Graffiti", only.Title);   // reported 14:05 on the 15th
    }

    [Fact]
    public async Task List_ToDateWithTime_ExcludesLaterIncidents()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var result = await service.GetIncidentsAsync(null, null, null, Utc(10, 15, 12, 0), 1, 20);

        Assert.Equal(new[] { "Bike stolen" }, result.Items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public async Task List_Paging_ReturnsRequestedPageAndTotals()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var page1 = await service.GetIncidentsAsync(null, null, null, null, 1, 2);
        var page2 = await service.GetIncidentsAsync(null, null, null, null, 2, 2);

        Assert.Equal(new[] { "Wallet taken", "Graffiti" }, page1.Items.Select(i => i.Title).ToArray());
        Assert.Equal(new[] { "Bike stolen" }, page2.Items.Select(i => i.Title).ToArray());
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page2.Page);
        Assert.Equal(2, page2.PageSize);
    }

    [Fact]
    public async Task List_PageBeyondTheEnd_ReturnsNoItemsButCorrectTotal()
    {
        using var db = NewContext();
        var service = await ServiceWithThreeIncidents(db);

        var result = await service.GetIncidentsAsync(null, null, null, null, 5, 20);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task List_SameTimestamp_OrderIsStable()
    {
        using var db = NewContext();
        db.Incidents.AddRange(
            NewIncident("One", Theft, Open, Utc(10, 14)),
            NewIncident("Two", Theft, Open, Utc(10, 14)));
        await db.SaveChangesAsync();
        var service = new EfIncidentQueryService(db);

        var first = await service.GetIncidentsAsync(null, null, null, null, 1, 20);
        var second = await service.GetIncidentsAsync(null, null, null, null, 1, 20);

        Assert.Equal(first.Items.Select(i => i.Id), second.Items.Select(i => i.Id));
    }
}