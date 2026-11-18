using CmSecurityService.Calls;
using CmSecurityService.Common;
using CmSecurityService.Controllers;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CmSecurityService.Tests;

/// <summary>
/// Tests for View 9-1-1 calls (AB#220): GET /api/security/v1/calls and GET /api/security/v1/calls/{id}.
/// </summary>
/// <remarks>
/// Seed data, newest first: 08 Police Received, 07 Fire Forwarded, 06 Police InProgress,
/// 05 EMS Failed, 04 Police Received, 03 EMS Forwarded, 02 Fire Forwarded, 01 Police Closed.
/// </remarks>
public class CallsControllerTests
{
    private static CallsController CreateController() =>
        new(new InMemoryCallQueryService());

    /// <summary>Builds the seed GUID for call number <paramref name="n"/> (1-8).</summary>
    private static Guid G(int n) => Guid.Parse($"11111111-1111-1111-1111-{n:D12}");

    private static PagedResult<CallDto> OkPage(ActionResult<PagedResult<CallDto>> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<PagedResult<CallDto>>(ok.Value);
    }

    [Fact]
    public async Task GetCalls_NoFilters_ReturnsAll_NewestFirst()
    {
        var page = OkPage(await CreateController().GetCalls(new CallQuery(), CancellationToken.None));

        Assert.Equal(8, page.TotalCount);
        Assert.Equal(8, page.Items.Count);
        Assert.Equal(G(8), page.Items[0].Id);
        Assert.True(page.Items.Zip(page.Items.Skip(1)).All(p => p.First.ReceivedAt >= p.Second.ReceivedAt));
    }

    [Theory]
    [InlineData("Received", 2)]
    [InlineData("inprogress", 1)]    // case-insensitive
    [InlineData("FORWARDED", 3)]
    [InlineData("Failed", 1)]
    [InlineData("closed", 1)]
    public async Task GetCalls_FilterByStatus_ReturnsOnlyThatStatus(string status, int expected)
    {
        var page = OkPage(await CreateController().GetCalls(new CallQuery { Status = status }, CancellationToken.None));

        Assert.Equal(expected, page.TotalCount);
        Assert.All(page.Items, c => Assert.Equal(status, c.Status, ignoreCase: true));
    }

    [Theory]
    [InlineData("Police", 4)]
    [InlineData("fire", 2)]
    [InlineData("EMS", 2)]
    public async Task GetCalls_FilterByType_ReturnsOnlyThatType(string type, int expected)
    {
        var page = OkPage(await CreateController().GetCalls(new CallQuery { Type = type }, CancellationToken.None));

        Assert.Equal(expected, page.TotalCount);
        Assert.All(page.Items, c => Assert.Equal(type, c.CallType, ignoreCase: true));
    }

    [Fact]
    public async Task GetCalls_FilterByStatusAndType_CombinesFilters()
    {
        var query = new CallQuery { Status = "Forwarded", Type = "Fire" };

        var page = OkPage(await CreateController().GetCalls(query, CancellationToken.None));

        Assert.Equal(new[] { G(7), G(2) }, page.Items.Select(c => c.Id).ToArray());
    }

    [Fact]
    public async Task GetCalls_FilterByDateRange_ToDateIncludesWholeDay()
    {
        var query = new CallQuery { From = new DateTime(2026, 11, 8), To = new DateTime(2026, 11, 10) };

        var page = OkPage(await CreateController().GetCalls(query, CancellationToken.None));

        Assert.Equal(new[] { G(7), G(6), G(5), G(4) }, page.Items.Select(c => c.Id).ToArray());
    }

    [Fact]
    public async Task GetCalls_Paging_ReturnsRequestedPageAndTotals()
    {
        var page = OkPage(await CreateController().GetCalls(new CallQuery { Page = 2, PageSize = 3 }, CancellationToken.None));

        Assert.Equal(new[] { G(5), G(4), G(3) }, page.Items.Select(c => c.Id).ToArray());
        Assert.Equal(8, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task GetCalls_PageBeyondEnd_ReturnsEmptyList()
    {
        var page = OkPage(await CreateController().GetCalls(new CallQuery { Page = 50 }, CancellationToken.None));

        Assert.Empty(page.Items);
        Assert.Equal(8, page.TotalCount);
    }

    [Theory]
    [InlineData(null, "Ambulance", 1, 20)]   // unknown type
    [InlineData("Pending", null, 1, 20)]     // unknown status
    [InlineData("1", null, 1, 20)]           // numeric status not allowed
    [InlineData(null, "0", 1, 20)]           // numeric type not allowed
    [InlineData(null, null, 0, 20)]          // page too low
    [InlineData(null, null, 1, 0)]           // pageSize too low
    [InlineData(null, null, 1, 101)]         // pageSize too high
    public async Task GetCalls_InvalidQuery_Returns400WithSharedErrorFormat(string? status, string? type, int pageNumber, int pageSize)
    {
        var query = new CallQuery { Status = status, Type = type, Page = pageNumber, PageSize = pageSize };

        var result = await CreateController().GetCalls(query, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var error = Assert.IsType<ApiError>(bad.Value);
        Assert.Equal(400, bad.StatusCode);
        Assert.Equal("invalid_query", error.Error);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public async Task GetCalls_FromAfterTo_Returns400()
    {
        var query = new CallQuery { From = new DateTime(2026, 11, 10), To = new DateTime(2026, 11, 1) };

        var result = await CreateController().GetCalls(query, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("invalid_query", Assert.IsType<ApiError>(bad.Value).Error);
    }

    [Fact]
    public async Task GetCallById_Existing_ReturnsCallWithStatus()
    {
        var result = await CreateController().GetCallById(G(2), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var call = Assert.IsType<CallDto>(ok.Value);
        Assert.Equal(G(2), call.Id);
        Assert.Equal("Forwarded", call.Status);
        Assert.Equal("Fire", call.CallType);
        Assert.True(call.ForwardedAt.HasValue);
    }

    [Fact]
    public async Task GetCallById_NotForwarded_HasNoForwardedAt()
    {
        var result = await CreateController().GetCallById(G(1), CancellationToken.None);

        var call = Assert.IsType<CallDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.False(call.ForwardedAt.HasValue);
    }

    [Fact]
    public async Task GetCallById_Missing_Returns404WithSharedErrorFormat()
    {
        var result = await CreateController().GetCallById(Guid.Empty, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ApiError>(notFound.Value);
        Assert.Equal(404, notFound.StatusCode);
        Assert.Equal("call_not_found", error.Error);
    }

    [Fact]
    public async Task AddCallViewing_ResolvedFromDI_ServesSeedData()
    {
        // Guards against DI picking the list constructor and injecting an empty list.
        var services = new ServiceCollection();
        services.AddCallViewing();
        using var provider = services.BuildServiceProvider();

        var service = provider.GetRequiredService<ICallQueryService>();
        var page = await service.GetCallsAsync(null, null, null, null, 1, 20);

        Assert.Equal(8, page.TotalCount);
    }

    [Fact]
    public void CallDto_DoesNotExposeOperatorNotes()
    {
        var names = typeof(CallDto).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain("OperatorNotes", names);
    }
}
