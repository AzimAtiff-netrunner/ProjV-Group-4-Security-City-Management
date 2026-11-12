using CmSecurityService.Incidents;
using Xunit;

namespace CmSecurityService.Tests;

public class IncidentServiceUnitTests
{
    private readonly InMemoryIncidentQueryService _service;

    public IncidentServiceUnitTests()
    {
        _service = new InMemoryIncidentQueryService();
    }

    [Fact]
    public async Task GetIncidentsAsync_ReturnsSeededIncidentsByDefault()
    {
        // Act
        var result = await _service.GetIncidentsAsync(
            status: null,
            type: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 20
        );

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 8);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task GetIncidentsAsync_FiltersByStatusCorrectly()
    {
        // Act
        var result = await _service.GetIncidentsAsync(
            status: IncidentStatus.Open,
            type: null,
            from: null,
            to: null,
            page: 1,
            pageSize: 20
        );

        // Assert
        Assert.NotNull(result);
        Assert.All(result.Items, item => Assert.Equal("Open", item.Status));
    }

    [Fact]
    public async Task AddIncidentAsync_IncrementsIdAndPersistsRecord()
    {
        // Arrange
        string title = "Unit Test Incident";
        string description = "Verifying add logic in memory service";
        string type = "Theft";
        string status = "Open";
        string location = "Sector 4";

        // Act
        var created = await _service.AddIncidentAsync(title, description, type, status, location);
        var retrieved = await _service.GetIncidentByIdAsync(created.Id);

        // Assert
        Assert.NotNull(created);
        Assert.True(created.Id > 8);
        Assert.Equal(title, created.Title);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetIncidentByIdAsync_ReturnsNullForNonExistentId()
    {
        // Act
        var result = await _service.GetIncidentByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }
}