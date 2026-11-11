using CmSecurityService.Calls;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Controllers;

/// <summary>
/// View 9-1-1 calls (Sprint 2).
/// </summary>
/// <remarks>
/// Read-only endpoints for dispatchers and other City Management services.
/// Creating and forwarding calls belong to other stories (POST /calls, Forward 9-1-1 calls);
/// their endpoints can be added to this controller later under the same route.
/// </remarks>
[ApiController]
[Route("api/security/v1/calls")]
[Produces("application/json")]
public class CallsController : ControllerBase
{
    private readonly ICallQueryService _calls;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="calls">Source of 9-1-1 call data.</param>
    public CallsController(ICallQueryService calls)
    {
        _calls = calls;
    }

    /// <summary>
    /// Lists 9-1-1 calls, newest first, with optional filters and paging.
    /// </summary>
    /// <param name="query">
    /// Filters: status (Received, Forwarded, Failed, Closed), type (Police, Fire, EMS),
    /// from, to (dates), page (default 1) and pageSize (default 20, max 100).
    /// </param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>200 with a page of calls; 400 if any filter is invalid.</returns>
    /// <response code="200">A page of calls.</response>
    /// <response code="400">One or more query values are invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CallDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<CallDto>>> GetCalls(
        [FromQuery] CallQuery query,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate(out var status, out var type);
        if (errors.Count > 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Invalid call query.",
                Detail = string.Join(" ", errors)
            });
        }

        var result = await _calls.GetCallsAsync(
            status, type, query.From, query.To, query.Page, query.PageSize, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets one 9-1-1 call by id.
    /// </summary>
    /// <param name="id">Call identifier (GUID).</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>200 with the call; 404 if it does not exist.</returns>
    /// <response code="200">The call.</response>
    /// <response code="404">No call has this id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CallDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CallDto>> GetCallById(Guid id, CancellationToken cancellationToken)
    {
        var call = await _calls.GetCallByIdAsync(id, cancellationToken);
        if (call is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Call not found.",
                Detail = $"No 9-1-1 call exists with id {id}."
            });
        }

        return Ok(call);
    }
}
