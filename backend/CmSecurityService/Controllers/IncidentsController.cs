using System.ComponentModel.DataAnnotations;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Controllers;

/// <summary>
/// Police incidents management (integrating US 216 and US 217).
/// </summary>
/// <remarks>
/// Provides read queries and retrieval for police incidents (#216), 
/// as well as new incident reporting and registration (#217).
/// </remarks>
[ApiController]
[Route("incidents")]
[Produces("application/json")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentQueryService _incidents;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="incidents">Source of incident data.</param>
    public IncidentsController(IIncidentQueryService incidents)
    {
        _incidents = incidents;
    }

    /// <summary>
    /// Payload structure for registering a new incident (US 217).
    /// </summary>
    /// <remarks>
    /// Title, Description, IncidentType and Location are required. Status is optional
    /// and defaults to "Open". IncidentType and Status must match existing names.
    /// </remarks>
    public record CreateIncidentRequest(
        [Required, StringLength(200, MinimumLength = 3)] string Title,
        [Required, StringLength(2000, MinimumLength = 10)] string Description,
        [Required, StringLength(100)] string IncidentType,
        [Required, StringLength(200)] string Location,
        [StringLength(50)] string? Status
    );

    /// <summary>
    /// Lists police incidents, newest first, with optional filters and paging (US 216).
    /// </summary>
    /// <param name="query">
    /// Filters: status (a status name such as Open or Closed), type, from, to (dates),
    /// page (default 1) and pageSize (default 20, max 100).
    /// </param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>200 with a page of incidents; 400 if any filter is invalid.</returns>
    /// <response code="200">A page of incidents.</response>
    /// <response code="400">One or more query values are invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IncidentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<IncidentDto>>> GetIncidents(
        [FromQuery] IncidentQuery query,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid incident query.",
                Detail = string.Join(" ", errors)
            });
        }

        try
        {
            var result = await _incidents.GetIncidentsAsync(
                query.Status, query.Type, query.From, query.To, query.Page, query.PageSize, cancellationToken);

            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid incident query.",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets one police incident by id (US 216).
    /// </summary>
    /// <param name="id">Incident identifier (GUID).</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>200 with the incident; 404 if it does not exist.</returns>
    /// <response code="200">The incident.</response>
    /// <response code="404">No incident has this id.</response>
    [HttpGet("{id:guid}")]
    [ActionName(nameof(GetIncidentById))]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> GetIncidentById(Guid id, CancellationToken cancellationToken)
    {
        var incident = await _incidents.GetIncidentByIdAsync(id, cancellationToken);
        if (incident is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Incident not found.",
                Detail = $"No incident exists with id {id}."
            });
        }

        return Ok(incident);
    }

    /// <summary>
    /// Registers a new police incident (US 217).
    /// </summary>
    /// <param name="request">Incident payload containing title, description, type, and location.</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>201 Created with the registered incident and location header.</returns>
    /// <response code="201">Incident successfully created.</response>
    /// <response code="400">Invalid payload submitted.</response>
    [HttpPost]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidentDto>> RegisterIncident(
        [FromBody] CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        // Field rules (required, lengths) are checked automatically by [ApiController]
        // before this method runs; invalid bodies never reach this point.
        try
        {
            var created = await _incidents.AddIncidentAsync(
                request.Title,
                request.Description,
                request.IncidentType,
                request.Status,
                request.Location,
                cancellationToken);

            return CreatedAtAction(nameof(GetIncidentById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Detail = ex.Message
            });
        }
    }
}