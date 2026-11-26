using CmSecurityService.Common;
using CmSecurityService.Controllers;
using CmSecurityService.Data;
using CmSecurityService.Models;
using CmSecurityService.Suspects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuspectPage = CmSecurityService.Incidents.PagedResult<CmSecurityService.Suspects.SuspectDto>;

namespace CmSecurityService.Tests;

/// <summary>
/// Tests for story #225, part 1: GET /api/security/v1/suspects and GET /suspects/{id}.
/// Uses the real EF service over EF Core's in-memory database.
/// </summary>
public class SuspectsControllerTests
{
    private static readonly Guid Marcus = Guid.Parse("22222222-2222-2222-2222-000000000001");
    private static readonly Guid Unknown = Guid.Parse("22222222-2222-2222-2222-000000000003");

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AppDbContext(options);
        db.Suspects.AddRange(
            NewSuspect(Marcus, "Marcus", "Hale", "Mac, M-Dog", "Under investigation", 2),
            NewSuspect(Guid.Parse("22222222-2222-2222-2222-000000000002"), "Lena", "Ortiz", null, "Charged", 4),
            NewSuspect(Unknown, null, null, "The Limper", "Under investigation", 6),
            NewSuspect(Guid.Parse("22222222-2222-2222-2222-000000000004"), "Darnell", "Price", "D", "Cleared", 8),
            NewSuspect(Guid.Parse("22222222-2222-2222-2222-000000000005"), "Priya", "Nair", null, "Under investigation", 10),
            NewSuspect(Guid.Parse("22222222-2222-2222-2222-000000000006"), "Tomas", "Hale", "Tom", "Wanted", 12));
        db.SaveChanges();
        return db;
    }

    private static Suspect NewSuspect(Guid id, string? first, string? last, string? aliases, string status, int day) => new()
    {
        SuspectId = id,
        FirstName = first,
        LastName = last,
        KnownAliases = aliases,
        Status = status,
        DateOfBirth = first is null ? null : new DateOnly(1990, 1, day),
        PhysicalDescription = "Description",
        CreatedAt = new DateTime(2026, 11, day, 12, 0, 0, DateTimeKind.Utc)
    };

    private static SuspectsController CreateController(AppDbContext db) =>
        new(new EfSuspectQueryService(db))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    private static async Task<SuspectPage> GetPage(SuspectQuery query)
    {
        using var db = NewContext();
        var result = await CreateController(db).GetSuspects(query, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsType<SuspectPage>(ok.Value);
    }

    private static async Task<ApiError> GetBadRequest(SuspectQuery query)
    {
        using var db = NewContext();
        var result = await CreateController(db).GetSuspects(query, CancellationToken.None);
        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        return Assert.IsType<ApiError>(bad.Value);
    }

    // ------------------------------------------------------------ list

    [Fact]
    public async Task List_NoFilters_ReturnsAllSuspects()
    {
        var page = await GetPage(new SuspectQuery());

        Assert.Equal(6, page.TotalCount);
        Assert.Equal(6, page.Items.Count);
    }

    [Fact]
    public async Task List_IsNewestFirst()
    {
        var page = await GetPage(new SuspectQuery());

        Assert.Equal("Tomas", page.Items[0].FirstName);
        Assert.Equal("Marcus", page.Items[^1].FirstName);
    }

    [Theory]
    [InlineData("hale", 2)]          // last name, two people
    [InlineData("PRIYA", 1)]         // first name, any case
    [InlineData("limper", 1)]        // alias of a suspect with no name
    [InlineData("  ortiz  ", 1)]     // spaces trimmed
    [InlineData("nobody", 0)]
    public async Task List_Search_MatchesNamesAndAliases(string search, int expected)
    {
        var page = await GetPage(new SuspectQuery { Search = search });

        Assert.Equal(expected, page.TotalCount);
    }

    [Theory]
    [InlineData("Under investigation", 3)]
    [InlineData("under INVESTIGATION", 3)]
    [InlineData("Wanted", 1)]
    [InlineData("Escaped", 0)]
    public async Task List_StatusFilter_IsCaseInsensitive(string status, int expected)
    {
        var page = await GetPage(new SuspectQuery { Status = status });

        Assert.Equal(expected, page.TotalCount);
    }

    [Fact]
    public async Task List_SearchAndStatus_Combine()
    {
        var page = await GetPage(new SuspectQuery { Search = "hale", Status = "Wanted" });

        var only = Assert.Single(page.Items);
        Assert.Equal("Tomas", only.FirstName);
    }

    [Fact]
    public async Task List_Paging_ReturnsRequestedPage()
    {
        var page = await GetPage(new SuspectQuery { Page = 2, PageSize = 4 });

        Assert.Equal(6, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(2, page.Items.Count);
    }

    [Fact]
    public async Task List_PageAfterTheEnd_IsEmpty()
    {
        var page = await GetPage(new SuspectQuery { Page = 5, PageSize = 4 });

        Assert.Empty(page.Items);
        Assert.Equal(6, page.TotalCount);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, 101, "pageSize")]
    public async Task List_InvalidPaging_Returns400InSharedFormat(int pageNumber, int pageSize, string field)
    {
        var error = await GetBadRequest(new SuspectQuery { Page = pageNumber, PageSize = pageSize });

        Assert.Equal("invalid_query", error.Error);
        Assert.Contains(field, error.Message);
    }

    [Fact]
    public async Task List_SearchTooLong_Returns400()
    {
        var error = await GetBadRequest(new SuspectQuery { Search = new string('a', 101) });

        Assert.Equal("invalid_query", error.Error);
        Assert.Contains("search", error.Message);
    }

    // ------------------------------------------------------------ by id

    [Fact]
    public async Task GetById_Existing_ReturnsSuspect()
    {
        using var db = NewContext();
        var result = await CreateController(db).GetSuspectById(Marcus, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var suspect = Assert.IsType<SuspectDto>(ok.Value);
        Assert.Equal("Hale", suspect.LastName);
        Assert.Equal("Mac, M-Dog", suspect.KnownAliases);
        Assert.Equal(new DateOnly(1990, 1, 2), suspect.DateOfBirth);
    }

    [Fact]
    public async Task GetById_SuspectWithUnknownName_ReturnsNullNames()
    {
        using var db = NewContext();
        var result = await CreateController(db).GetSuspectById(Unknown, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var suspect = Assert.IsType<SuspectDto>(ok.Value);
        Assert.Null(suspect.FirstName);
        Assert.Null(suspect.DateOfBirth);
        Assert.Equal("The Limper", suspect.KnownAliases);
    }

    [Fact]
    public async Task GetById_Missing_Returns404InSharedFormat()
    {
        using var db = NewContext();
        var id = Guid.NewGuid();
        var result = await CreateController(db).GetSuspectById(id, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var error = Assert.IsType<ApiError>(notFound.Value);
        Assert.Equal("suspect_not_found", error.Error);
        Assert.Contains(id.ToString(), error.Message);
    }
}
