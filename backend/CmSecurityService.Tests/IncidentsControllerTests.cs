using CmSecurityService.Controllers;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CmSecurityService.Tests;

/// <summary>
/// Tests for View police incidents (#216): GET /incidents and GET /incidents/{id}.
/// </summary>
public class IncidentsControllerTests
{
    private static IncidentsController CreateController() =>
        new(new InMemoryIncidentQueryService());

    private static PagedResult<IncidentDto> OkPage(ActionResult<PagedResult<IncidentDto>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<PagedResult<IncidentDto>>(ok.Value);
    }

    [Fact]
    public async Task GetIncidents_NoFilters_ReturnsAllNonDeleted_NewestFirst()
    {
        var page = OkPage(await CreateController().GetIncidents(new IncidentQuery(), CancellationToken.None));

        Assert.Equal(8, page.TotalCount);               // 9 seeded, 1 soft-deleted
        Assert.Equal(8, page.Items.Count);
        Assert.Equal(8, page.Items[0].Id);              // most recent
        Assert.True(page.Items.Zip(page.Items.Skip(1)).All(p => p.First.ReportedAt >= p.Second.ReportedAt));
    }

    [Fact]
    public async Task GetIncidents_DeletedIncident_IsNeverReturned()
    {
        var page = OkPage(await CreateController().GetIncidents(new IncidentQuery(), CancellationToken.None));

        Assert.DoesNotContain(page.Items, i => i.Id == 9);
    }

    [Theory]
    [InlineData("Open", 3)]
    [InlineData("underinvestigation", 2)]   // case-insensitive
    [InlineData("CLOSED", 3)]
    public async Task GetIncidents_FilterByStatus_ReturnsOnlyThatStatus(string status, int expected)
    {
        var page = OkPage(await CreateController().GetIncidents(new IncidentQuery { Status = status }, CancellationToken.None));

        Assert.Equal(expected, page.TotalCount);
        Assert.All(page.Items, i => Assert.Equal(status, i.Status, ignoreCase: true));
    }

    [Fact]
    public async Task GetIncidents_FilterByType_IsCaseInsensitive()
    {
        var page = OkPage(await CreateController().GetIncidents(new IncidentQuery { Type = "theft" }, CancellationToken.None));

        Assert.Equal(2, page.TotalCount);
        Assert.All(page.Items, i => Assert.Equal("Theft", i.IncidentType));
    }

    [Fact]
    public async Task GetIncidents_FilterByDateRange_ToDateIncludesWholeDay()
    {
        var query = new IncidentQuery { From = new DateTime(2026, 10, 16), To = new DateTime(2026, 10, 18) };

        var page = OkPage(await CreateController().GetIncidents(query, CancellationToken.None));

        Assert.Equal(new[] { 5, 4, 3 }, page.Items.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task GetIncidents_Paging_ReturnsRequestedPageAndTotals()
    {
        var page = OkPage(await CreateController().GetIncidents(new IncidentQuery { Page = 2, PageSize = 3 }, CancellationToken.None));

        Assert.Equal(new[] { 5, 4, 3 }, page.Items.Select(i => i.Id).ToArray());
        Assert.Equal(8, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task GetIncidents_PageBeyondEnd_ReturnsEmptyList()
    {
        var page = OkPage(await CreateController().GetIncidents(new IncidentQuery { Page = 50 }, CancellationToken.None));

        Assert.Empty(page.Items);
        Assert.Equal(8, page.TotalCount);
    }

    [Theory]
    [InlineData("Pending", 1, 20)]   // unknown status
    [InlineData("1", 1, 20)]         // numeric status not allowed
    [InlineData(null, 0, 20)]        // page too low
    [InlineData(null, 1, 0)]         // pageSize too low
    [InlineData(null, 1, 101)]       // pageSize too high
    public async Task GetIncidents_InvalidQuery_Returns400(string? status, int pageNumber, int pageSize)
    {
        var query = new IncidentQuery { Status = status, Page = pageNumber, PageSize = pageSize };

        var result = await CreateController().GetIncidents(query, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(bad.Value);
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
    }

    [Fact]
    public async Task GetIncidents_FromAfterTo_Returns400()
    {
        var query = new IncidentQuery { From = new DateTime(2026, 10, 20), To = new DateTime(2026, 10, 1) };

        var result = await CreateController().GetIncidents(query, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetIncidentById_Existing_ReturnsIncident()
    {
        var result = await CreateController().GetIncidentById(3, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var incident = Assert.IsType<IncidentDto>(ok.Value);
        Assert.Equal(3, incident.Id);
        Assert.Equal("UnderInvestigation", incident.Status);
    }

    [Theory]
    [InlineData(999)]   // never existed
    [InlineData(9)]     // soft-deleted
    [InlineData(-1)]
    public async Task GetIncidentById_Missing_Returns404(int id)
    {
        var result = await CreateController().GetIncidentById(id, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task AddIncidentViewing_ResolvedFromDI_ServesSeedData()
    {
        // Regression: DI once picked the list constructor and injected an empty list,
        // so the running API returned zero incidents while direct-construction tests passed.
        var services = new ServiceCollection();
        services.AddIncidentViewing();
        using var provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<IIncidentQueryService>();
        var page = await service.GetIncidentsAsync(null, null, null, null, 1, 20);

        Assert.Equal(8, page.TotalCount);
    }

    [Fact]
    public void IncidentDto_DoesNotExposeInternalFields()
    {
        var names = typeof(IncidentDto).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("InternalNotes", names);
        Assert.DoesNotContain("IsDeleted", names);
    }
}
