using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using CmSecurityService.Controllers;
using CmSecurityService.Data;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;
using IncidentRow = CmSecurityService.Models.Incident;
using IncidentStatusRow = CmSecurityService.Models.IncidentStatus;   // entity; Incidents.IncidentStatus is an enum
using IncidentTypeRow = CmSecurityService.Models.IncidentType;
using CmSecurityService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
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
        builder.UseSetting("Jwt:Secret", "test-only-signing-key-at-least-32-characters-long");
        builder.UseSetting("Jwt:Issuer", "CmRegistrationService");
        builder.UseSetting("Jwt:Audience", "CmRegistrationClients");

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

    private static async Task Seed(AppDbContext db)
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

        // Seed roles for authorization tests.
        db.Roles.AddRange(
            new Role { RoleId = 1, Name = "Police Officer" },
            new Role { RoleId = 2, Name = "Security Administrator" }
        );

        // Test user IDs must match the JWT "sub" claim.
        const string adminUserId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        const string officerUserId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
        const string inactiveAdminUserId = "cccccccc-cccc-cccc-cccc-cccccccccccc";
        const string noRoleUserId = "dddddddd-dddd-dddd-dddd-dddddddddddd";

        const string adminOfficerId = "11111111-1111-1111-1111-111111111111";
        const string officerOfficerId = "22222222-2222-2222-2222-222222222222";
        const string inactiveAdminOfficerId = "33333333-3333-3333-3333-333333333333";
        const string noRoleOfficerId = "44444444-4444-4444-4444-444444444444";

        db.Officers.AddRange(
            new Officer
            {
                OfficerId = adminOfficerId,
                UserId = adminUserId,
                BadgeNumber = "AUTH001",
                FirstName = "Test",
                LastName = "Adminstrator",
                IsActive = true
            },
            new Officer
            {
                OfficerId = officerOfficerId,
                UserId = officerUserId,
                BadgeNumber = "AUTH002",
                FirstName = "Test",
                LastName = "Officer",
                IsActive = true
            },
            new Officer
            {
                OfficerId = inactiveAdminOfficerId,
                UserId = inactiveAdminUserId,
                BadgeNumber = "AUTH003",
                FirstName = "Inactive",
                LastName = "Administrator",
                IsActive = false
            },
            new Officer
            {
                OfficerId = noRoleOfficerId,
                UserId = noRoleUserId,
                BadgeNumber = "AUTH004",
                FirstName = "No",
                LastName = "Role",
                IsActive = true
            },
            new Officer
            {
                OfficerId = "55555555-5555-5555-5555-555555555555",
                UserId = null,
                BadgeNumber = "AUTH005",
                FirstName = "Target",
                LastName = "Officer",
                IsActive = true
            }
        );

        db.OfficerRoles.AddRange(
            new OfficerRole
            {
                OfficerRoleId = Guid.NewGuid().ToString(),
                OfficerId = adminOfficerId,
                RoleId = 2,
                AssignedAt = DateTime.UtcNow
            },
            new OfficerRole
            {
                OfficerRoleId = Guid.NewGuid().ToString(),
                OfficerId = officerOfficerId,
                RoleId = 1,
                AssignedAt = DateTime.UtcNow
            },
            new OfficerRole
            {
                OfficerRoleId = Guid.NewGuid().ToString(),
                OfficerId = inactiveAdminOfficerId,
                RoleId = 2,
                AssignedAt = DateTime.UtcNow
            }
        );

        db.SaveChanges();
    }
    public static string CreateToken(string userId, bool expired = false)
    {
        const string secret = "test-only-signing-key-at-least-32-characters-long";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = expired
            ? DateTime.UtcNow.AddMinutes(-5)
            : DateTime.UtcNow.AddMinutes(5);

        var token = new JwtSecurityToken(
            issuer: "CmRegistrationService",
            audience: "CmRegistrationClients",
            claims: new[]
            {
                new Claim("sub", userId)
            },
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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
        private const string RoleAssignmentUrl =
        "/api/security/v1/personnel/55555555-5555-5555-5555-555555555555/role";

        private const string AdminUserId =
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

        private const string OfficerUserId =
            "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

        private const string InactiveAdminUserId =
            "cccccccc-cccc-cccc-cccc-cccccccccccc";

        private const string NoRoleUserId =
            "dddddddd-dddd-dddd-dddd-dddddddddddd";

        private async Task SetAuthorizationToken(string userId)
        {
            var token = IncidentsApiFactory.CreateToken(userId);

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        }

        [Fact]
        public async Task AssignRole_NoToken_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.PutAsJsonAsync(
                RoleAssignmentUrl,
                new { roleName = "Police Officer" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_InvalidToken_ReturnsUnauthorized()
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "invalid-token");

            var response = await _client.PutAsJsonAsync(
                RoleAssignmentUrl,
                new { roleName = "Police Officer" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_PoliceOfficer_ReturnsForbidden()
        {
            SetAuthorizationToken(OfficerUserId);

            var response = await _client.PutAsJsonAsync(
                RoleAssignmentUrl,
                new { roleName = "Police Officer" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_InactiveAdministrator_ReturnsForbidden()
        {
            SetAuthorizationToken(InactiveAdminUserId);

            var response = await _client.PutAsJsonAsync(
                RoleAssignmentUrl,
                new { roleName = "Police Officer" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AssignRole_OfficerWithoutRole_ReturnsForbidden()
        {
            SetAuthorizationToken(NoRoleUserId);

            var response = await _client.PutAsJsonAsync(
                RoleAssignmentUrl,
                new { roleName = "Police Officer" });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        [Fact]
        public async Task AssignRole_ActiveAdministrator_ReturnsOk()
        {
            SetAuthorizationToken(AdminUserId);

            var response = await _client.PutAsJsonAsync(
                RoleAssignmentUrl,
                new { roleName = "Police Officer" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
}
