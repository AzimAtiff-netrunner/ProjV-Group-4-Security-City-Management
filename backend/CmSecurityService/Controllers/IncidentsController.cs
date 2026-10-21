using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Controllers;

/// <summary>
/// View police incidents (user story #216).
/// </summary>
/// <remarks>
/// Read-only endpoints for officers and other City Management services
/// (e.g. Residence checking whether a case is under investigation).
/// Creating incidents is handled by Register police incidents (#217).
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
    /// Lists police incidents, newest first, with optional filters and paging.
    /// </summary>
    /// <param name="query">
    /// Filters: status (Open, UnderInvestigation, Closed), type, from, to (dates),
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
        var errors = query.Validate(out var status);
        if (errors.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid incident query.",
                Detail = string.Join(" ", errors)
            });
        }

        var result = await _incidents.GetIncidentsAsync(
            status, query.Type, query.From, query.To, query.Page, query.PageSize, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets one police incident by id.
    /// </summary>
    /// <param name="id">Incident identifier.</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>200 with the incident; 404 if it does not exist.</returns>
    /// <response code="200">The incident.</response>
    /// <response code="404">No incident has this id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncidentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidentDto>> GetIncidentById(int id, CancellationToken cancellationToken)
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
}
