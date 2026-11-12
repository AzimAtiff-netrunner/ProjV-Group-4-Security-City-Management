using System.ComponentModel.DataAnnotations;
using CmSecurityService.Controllers;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Tests;

/// <summary>
/// Tests that the controller turns service results and errors into the right HTTP responses
/// (acceptance criterion 2: invalid input is rejected with an HTTP error and a clear message).
/// </summary>
public class IncidentsControllerTests
{
    /// <summary>Hand-written stand-in for the service, so no database is needed.</summary>
    private sealed class FakeIncidentService : IIncidentQueryService
    {
        public PagedResult<IncidentDto> ListResult { get; set; } = new();
        public IncidentDto? ByIdResult { get; set; }
        public IncidentDto? AddResult { get; set; }
        public Exception? ThrowOnList { get; set; }
        public Exception? ThrowOnAdd { get; set; }

        public bool ListCalled { get; private set; }
        public string? LastStatusFilter { get; private set; }
        public string? LastTypeFilter { get; private set; }
        public Guid LastRequestedId { get; private set; }
        public (string Title, string Description, string Type, string? Status, string Location)? LastAdd { get; private set; }

        public Task<PagedResult<IncidentDto>> GetIncidentsAsync(
            string? status, string? type, DateTime? from, DateTime? to,
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            ListCalled = true;
            LastStatusFilter = status;
            LastTypeFilter = type;
            if (ThrowOnList is not null) throw ThrowOnList;
            return Task.FromResult(ListResult);
        }

        public Task<IncidentDto?> GetIncidentByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            LastRequestedId = id;
            return Task.FromResult(ByIdResult);
        }

        public Task<IncidentDto> AddIncidentAsync(
            string title, string description, string incidentType, string? status, string location,
            CancellationToken cancellationToken = default)
        {
            LastAdd = (title, description, incidentType, status, location);
            if (ThrowOnAdd is not null) throw ThrowOnAdd;
            return Task.FromResult(AddResult!);
        }
    }

    private static IncidentDto SampleDto() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Bike stolen",
        Description = "Bike taken from the library rack.",
        IncidentType = "Theft",
        Status = "Open",
        Location = "Downtown",
        ReportedAt = new DateTime(2026, 10, 14, 9, 30, 0, DateTimeKind.Utc)
    };

    private static IncidentsController.CreateIncidentRequest SampleRequest() => new(
        Title: "Bike stolen",
        Description: "Bike taken from the library rack.",
        IncidentType: "Theft",
        Location: "Downtown",
        Status: "Open");

    // ------------------------------------------------------------------ GET /incidents

    [Fact]
    public async Task GetIncidents_ValidQuery_ReturnsOkWithPage()
    {
        var service = new FakeIncidentService
        {
            ListResult = new PagedResult<IncidentDto> { Items = new[] { SampleDto() }, Page = 1, PageSize = 20, TotalCount = 1 }
        };
        var controller = new IncidentsController(service);

        var result = await controller.GetIncidents(new IncidentQuery { Status = "Open", Type = "Theft" }, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(service.ListResult, ok.Value);
        Assert.Equal("Open", service.LastStatusFilter);
        Assert.Equal("Theft", service.LastTypeFilter);
    }

    [Fact]
    public async Task GetIncidents_InvalidQuery_Returns400AndSkipsTheService()
    {
        var service = new FakeIncidentService();
        var controller = new IncidentsController(service);

        var result = await controller.GetIncidents(new IncidentQuery { Page = 0, PageSize = 500 }, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(bad.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Contains("page must be 1 or greater", problem.Detail);
        Assert.Contains("pageSize must be between 1 and", problem.Detail);
        Assert.False(service.ListCalled);
    }

    [Fact]
    public async Task GetIncidents_UnknownStatus_Returns400WithTheServiceMessage()
    {
        var service = new FakeIncidentService
        {
            ThrowOnList = new ValidationException("status 'Banana' is not valid.")
        };
        var controller = new IncidentsController(service);

        var result = await controller.GetIncidents(new IncidentQuery { Status = "Banana" }, default);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(bad.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("status 'Banana' is not valid.", problem.Detail);
    }

    // ------------------------------------------------------------ GET /incidents/{id}

    [Fact]
    public async Task GetIncidentById_Found_ReturnsOk()
    {
        var dto = SampleDto();
        var service = new FakeIncidentService { ByIdResult = dto };
        var controller = new IncidentsController(service);

        var result = await controller.GetIncidentById(dto.Id, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(dto, ok.Value);
        Assert.Equal(dto.Id, service.LastRequestedId);
    }

    [Fact]
    public async Task GetIncidentById_NotFound_Returns404WithMessage()
    {
        var service = new FakeIncidentService { ByIdResult = null };
        var controller = new IncidentsController(service);
        var id = Guid.NewGuid();

        var result = await controller.GetIncidentById(id, default);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Contains(id.ToString(), problem.Detail);
    }

    // ------------------------------------------------------------------ POST /incidents

    [Fact]
    public async Task RegisterIncident_Valid_Returns201PointingAtTheNewIncident()
    {
        var dto = SampleDto();
        var service = new FakeIncidentService { AddResult = dto };
        var controller = new IncidentsController(service);

        var result = await controller.RegisterIncident(SampleRequest(), default);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(IncidentsController.GetIncidentById), created.ActionName);
        Assert.Equal(dto.Id, created.RouteValues!["id"]);
        Assert.Same(dto, created.Value);
    }

    [Fact]
    public async Task RegisterIncident_PassesTheRequestFieldsToTheService()
    {
        var service = new FakeIncidentService { AddResult = SampleDto() };
        var controller = new IncidentsController(service);

        await controller.RegisterIncident(
            new IncidentsController.CreateIncidentRequest(
                Title: "A title",
                Description: "A long enough description.",
                IncidentType: "Vandalism",
                Location: "Uptown",
                Status: null),
            default);

        Assert.NotNull(service.LastAdd);
        var sent = service.LastAdd!.Value;
        Assert.Equal("A title", sent.Title);
        Assert.Equal("A long enough description.", sent.Description);
        Assert.Equal("Vandalism", sent.Type);
        Assert.Null(sent.Status);
        Assert.Equal("Uptown", sent.Location);
    }

    [Fact]
    public async Task RegisterIncident_UnknownTypeOrStatus_Returns400WithTheServiceMessage()
    {
        var service = new FakeIncidentService
        {
            ThrowOnAdd = new ValidationException("Unknown incident type 'Banana'.")
        };
        var controller = new IncidentsController(service);

        var result = await controller.RegisterIncident(SampleRequest(), default);

        var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetails>(bad.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Validation Failed", problem.Title);
        Assert.Equal("Unknown incident type 'Banana'.", problem.Detail);
    }
}