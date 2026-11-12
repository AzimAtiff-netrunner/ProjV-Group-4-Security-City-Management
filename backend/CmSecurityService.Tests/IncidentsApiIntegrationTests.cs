using CmSecurityService.Controllers;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CmSecurityService.Tests;

public class IncidentsApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public IncidentsApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Incidents_ReturnsOkWithPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/security/v1/incidents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<PagedResult<IncidentDto>>(_jsonOptions);
        Assert.NotNull(content);
        Assert.True(content.TotalCount >= 8);
    }

    [Fact]
    public async Task Get_IncidentById_ReturnsOkForExistingIncident()
    {
        // Act
        var response = await _client.GetAsync("/api/security/v1/incidents/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var incident = await response.Content.ReadFromJsonAsync<IncidentDto>(_jsonOptions);
        Assert.NotNull(incident);
        Assert.Equal(1, incident.Id);
    }

    [Fact]
    public async Task Get_IncidentById_ReturnsNotFoundForInvalidId()
    {
        // Act
        var response = await _client.GetAsync("/api/security/v1/incidents/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_RegisterIncident_FailsWhenTitleIsMissing()
    {
        // Arrange
        var invalidPayload = new IncidentsController.CreateIncidentRequest(
            Title: "",
            Description: "Missing title test",
            IncidentType: "Traffic",
            Location: "Downtown",
            Status: "Open"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/security/v1/incidents", invalidPayload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_RegisterIncident_CreatesRecordAndMakesItQueryable()
    {
        // Arrange (US 217)
        var newIncident = new IncidentsController.CreateIncidentRequest(
            Title: "Integration Test Hazard",
            Description: "Simulated chemical spill for integration verification",
            IncidentType: "Emergency",
            Location: "Industrial Park",
            Status: "Open"
        );

        // Act 1: Post
        var postResponse = await _client.PostAsJsonAsync("/api/security/v1/incidents", newIncident);

        // Assert 1: Status 201 Created
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<IncidentDto>(_jsonOptions);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);

        // Act 2: Query by ID (US 216)
        var getByIdResponse = await _client.GetAsync($"/api/security/v1/incidents/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        // Act 3: Verify it appears in the catalog list
        var getAllResponse = await _client.GetAsync("/api/security/v1/incidents");
        var catalog = await getAllResponse.Content.ReadFromJsonAsync<PagedResult<IncidentDto>>(_jsonOptions);
        Assert.NotNull(catalog);
        Assert.Contains(catalog.Items, item => item.Id == created.Id);
    }
}
