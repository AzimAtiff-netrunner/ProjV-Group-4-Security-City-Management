using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CmSecurityService.Controllers;
using CmSecurityService.Data;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using IncidentRow = CmSecurityService.Models.Incident;
using IncidentStatusRow = CmSecurityService.Models.IncidentStatus;   // entity; Incidents.IncidentStatus is an enum
using IncidentTypeRow = CmSecurityService.Models.IncidentType;

namespace CmSecurityService.Tests;

/// <summary>
/// Runs the real app against an in-memory database instead of MySQL.
/// </summary>
public class IncidentsApiFactory : WebApplicationFactory<Program>
{
    // Shared root so the seeding context and the app's contexts see the same data.
    private static readonly InMemoryDatabaseRoot DbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Program.cs throws when this is missing; the value is never used to connect.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("incidents-tests", DbRoot));

            using var scope = services.BuildServiceProvider().CreateScope();
            Seed(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        });
    }

    private static void Seed(AppDbContext db)
    {
        // Ids of types and statuses are left for EF to generate.
        var types = new[] { "Theft", "Vandalism", "Burglary", "Traffic", "Disturbance", "Emergency" }
            .ToDictionary(n => n, n => new IncidentTypeRow { Name = n, Description = n });
        var statuses = new[] { "Open", "UnderInvestigation", "Closed" }
            .ToDictionary(n => n, n => new IncidentStatusRow { Name = n, Description = n });

        db.IncidentTypes.AddRange(types.Values);
        db.IncidentStatuses.AddRange(statuses.Values);
        db.SaveChanges();

        // (n, title, type, status, location, reportedAt) - same data as InMemoryIncidentQueryService
        var rows = new (int N, string Title, string Type, string Status, string Location, DateTime At)[]
        {
            (1, "Bike stolen from rack",        "Theft",       "Open",               "Downtown",  new(2026, 10, 14, 9, 30, 0, DateTimeKind.Utc)),
            (2, "Graffiti on bus shelter",      "Vandalism",   "Closed",             "Uptown",    new(2026, 10, 15, 14, 5, 0, DateTimeKind.Utc)),
            (3, "Break-in at convenience store","Burglary",    "UnderInvestigation", "Eastside",  new(2026, 10, 16, 6, 45, 0, DateTimeKind.Utc)),
            (4, "Two-car collision",            "Traffic",     "Closed",             "Westside",  new(2026, 10, 17, 17, 20, 0, DateTimeKind.Utc)),
            (5, "Noise complaint",              "Disturbance", "Closed",             "Northgate", new(2026, 10, 18, 4, 10, 0, DateTimeKind.Utc)),
            (6, "Wallet taken on transit",      "Theft",       "UnderInvestigation", "Downtown",  new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc)),
            (7, "Attempted break-in",           "Burglary",    "Open",               "Eastside",  new(2026, 10, 20, 7, 55, 0, DateTimeKind.Utc)),
            (8, "Abandoned vehicle",            "Traffic",     "Open",               "Westside",  new(2026, 10, 21, 10, 15, 0, DateTimeKind.Utc)),
        };

        foreach (var r in rows)
        {
            db.Incidents.Add(new IncidentRow
            {
                IncidentId = Guid.Parse($"00000001-0000-0000-0000-{r.N:D12}"),
                IncidentTypeId = types[r.Type].IncidentTypeId,
                IncidentStatusId = statuses[r.Status].IncidentStatusId,
                Title = r.Title,
                Description = $"Seeded incident: {r.Title}",
                Location = r.Location,
                CreatedAt = r.At
            });
        }

        db.SaveChanges();
    }
}

public class IncidentsApiIntegrationTests : IClassFixture<IncidentsApiFactory>
{
    private const string BaseUrl = "/api/security/v1/incidents";

    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Guid _existingIncidentId = Guid.Parse("00000001-0000-0000-0000-000000000001");

    public IncidentsApiIntegrationTests(IncidentsApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Incidents_ReturnsOkWithPagedResult()
    {
        var response = await _client.GetAsync(BaseUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<IncidentDto>>(_jsonOptions);
        Assert.NotNull(content);
        Assert.True(content.TotalCount >= 8);
    }

    [Fact]
    public async Task Get_IncidentById_ReturnsOkForExistingIncident()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{_existingIncidentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var incident = await response.Content.ReadFromJsonAsync<IncidentDto>(_jsonOptions);
        Assert.NotNull(incident);
        Assert.Equal(_existingIncidentId, incident.Id);
    }

    [Fact]
    public async Task Get_IncidentById_ReturnsNotFoundForUnknownId()
    {
        var response = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_RegisterIncident_FailsWhenTitleIsMissing()
    {
        var invalidPayload = new IncidentsController.CreateIncidentRequest(
            Title: "",
            Description: "Missing title test",
            IncidentType: "Traffic",
            Location: "Downtown",
            Status: "Open"
        );

        var response = await _client.PostAsJsonAsync(BaseUrl, invalidPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_RegisterIncident_CreatesRecordAndMakesItQueryable()
    {
        // US 217
        var newIncident = new IncidentsController.CreateIncidentRequest(
            Title: "Integration Test Hazard",
            Description: "Simulated chemical spill for integration verification",
            IncidentType: "Emergency",
            Location: "Industrial Park",
            Status: "Open"
        );

        var postResponse = await _client.PostAsJsonAsync(BaseUrl, newIncident);

        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<IncidentDto>(_jsonOptions);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        // US 216: fetch by id
        var getByIdResponse = await _client.GetAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        // Appears in the list (newest first, so it is on page 1)
        var getAllResponse = await _client.GetAsync(BaseUrl);
        var catalog = await getAllResponse.Content.ReadFromJsonAsync<PagedResult<IncidentDto>>(_jsonOptions);
        Assert.NotNull(catalog);
        Assert.Contains(catalog.Items, item => item.Id == created.Id);
    }
}
