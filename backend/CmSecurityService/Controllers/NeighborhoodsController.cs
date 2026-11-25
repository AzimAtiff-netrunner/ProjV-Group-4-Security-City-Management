using CmSecurityService.Common;
using CmSecurityService.Suspects;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Controllers;

/// <summary>
/// Neighborhood crime rankings (story #225).
/// </summary>
[ApiController]
[Route("api/security/v1/neighborhoods")]
[Produces("application/json")]
public class NeighborhoodsController : ControllerBase
{
    private readonly ISuspectQueryService _service;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="service">Source of incident counts.</param>
    public NeighborhoodsController(ISuspectQueryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Ranks neighborhoods by number of reported incidents, highest first.
    /// </summary>
    /// <param name="query">from, to (dates, optional) and top (default 10, max 50).</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>200 with the ranking; 400 if a value is invalid.</returns>
    /// <response code="200">Neighborhoods ranked by incident count. Equal counts share a rank.</response>
    /// <response code="400">One or more query values are invalid.</response>
    [HttpGet("crime-rankings")]
    [ProducesResponseType(typeof(IReadOnlyList<NeighborhoodRankingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NeighborhoodRankingDto>>> GetCrimeRankings(
        [FromQuery] CrimeRankingQuery query,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate();
        if (errors.Count > 0)
        {
            return BadRequest(ApiError.Create(HttpContext, "invalid_query", string.Join(" ", errors)));
        }

        var rankings = await _service.GetCrimeRankingsAsync(query.From, query.To, query.Top, cancellationToken);
        return Ok(rankings);
    }
}
