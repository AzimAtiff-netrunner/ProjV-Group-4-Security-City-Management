using CmSecurityService.Common;
using CmSecurityService.Incidents;
using CmSecurityService.Suspects;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Controllers;

/// <summary>
/// View suspect records (story #225).
/// </summary>
[ApiController]
[Route("api/security/v1/suspects")]
[Produces("application/json")]
public class SuspectsController : ControllerBase
{
    private readonly ISuspectQueryService _suspects;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="suspects">Source of suspect data.</param>
    public SuspectsController(ISuspectQueryService suspects)
    {
        _suspects = suspects;
    }

    /// <summary>
    /// Lists suspects, newest first, with optional search, status filter and paging.
    /// </summary>
    /// <param name="query">
    /// search (name or alias), status (e.g. "Under investigation"),
    /// page (default 1) and pageSize (default 20, max 100).
    /// </param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>200 with a page of suspects; 400 if a value is invalid.</returns>
    /// <response code="200">A page of suspects.</response>
    /// <response code="400">One or more query values are invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<SuspectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<SuspectDto>>> GetSuspects(
        [FromQuery] SuspectQuery query,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return BadRequest(ApiError.Create(HttpContext, "invalid_query", string.Join(" ", errors)));
        }

        var result = await _suspects.GetSuspectsAsync(
            query.Search, query.Status, query.Page, query.PageSize, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets one suspect by id.
    /// </summary>
    /// <param name="id">Suspect identifier (GUID).</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>200 with the suspect; 404 if it does not exist.</returns>
    /// <response code="200">The suspect.</response>
    /// <response code="404">No suspect has this id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SuspectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SuspectDto>> GetSuspectById(Guid id, CancellationToken cancellationToken)
    {
        var suspect = await _suspects.GetSuspectByIdAsync(id, cancellationToken);
        if (suspect is null)
        {
            return NotFound(ApiError.Create(HttpContext, "suspect_not_found", $"No suspect exists with id {id}."));
        }

        return Ok(suspect);
    }
}
