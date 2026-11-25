using CmSecurityService.Common;
using CmSecurityService.Controllers;
using CmSecurityService.Data;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidentPage = CmSecurityService.Incidents.PagedResult<CmSecurityService.Incidents.IncidentDto>;
using IncidentRow = CmSecurityService.Models.Incident;
using IncidentTypeRow = CmSecurityService.Models.IncidentType;
using StatusRow = CmSecurityService.Models.IncidentStatus;

namespace CmSecurityService.Tests;

/// <summary>
/// Bug fix: GET /api/security/v1/incidents?type=... with an unknown type returned 200 and an
/// empty list, while an unknown status returned 400. Both now return 400 invalid_query.
/// </summary>
public class IncidentTypeFilterTests
{
    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.IncidentTypes.AddRange(
            new IncidentTypeRow { IncidentTypeId = 1, Name = "Theft" },
            new IncidentTypeRow { IncidentTypeId = 2, Name = "Vandalism" });
        db.IncidentStatuses.Add(new StatusRow { IncidentStatusId = 1, Name = "Open" });
        db.Incidents.Add(new IncidentRow
        {
            IncidentId = Guid.NewGuid(),
            IncidentTypeId = 1,
            IncidentStatusId = 1,
            Title = "Bike stolen",
            Description = "Bike taken from the rack.",
            Location = "Downtown",
            CreatedAt = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc)
        });
        db.SaveChanges();
        return db;
    }

    private static IncidentsController CreateController(AppDbContext db) =>
        new(new EfIncidentQueryService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    [Fact]
    public async Task UnknownType_Returns400InvalidQuery_ListingValidTypes()
    {
        using var db = NewContext();
        var result = await CreateController(db).GetIncidents(new IncidentQuery { Type = "Robbery" }, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ApiError>(bad.Value);
        Assert.Equal("invalid_query", error.Error);
        Assert.Contains("Robbery", error.Message);
        Assert.Contains("Theft", error.Message);
        Assert.Contains("Vandalism", error.Message);
    }

    [Fact]
    public async Task KnownType_StillReturnsMatchingIncidents()
    {
        using var db = NewContext();
        var result = await CreateController(db).GetIncidents(new IncidentQuery { Type = "Theft" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<IncidentPage>(ok.Value);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task KnownTypeWithNoIncidents_ReturnsEmptyList_Not400()
    {
        using var db = NewContext();
        var result = await CreateController(db).GetIncidents(new IncidentQuery { Type = "Vandalism" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<IncidentPage>(ok.Value);
        Assert.Empty(page.Items);
    }
}
