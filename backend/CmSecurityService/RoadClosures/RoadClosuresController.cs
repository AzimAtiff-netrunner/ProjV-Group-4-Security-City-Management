using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.RoadClosures;

[ApiController]
[Route("api/security/v1/road-closures")]
public class RoadClosuresController : ControllerBase
{
    private readonly IRoadClosureService _roadClosureService;

    public RoadClosuresController(IRoadClosureService roadClosureService)
    {
        _roadClosureService = roadClosureService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoadClosureResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveClosures(
        CancellationToken cancellationToken)
    {
        var closures = await _roadClosureService.GetActiveClosuresAsync(
            cancellationToken);

        return Ok(closures);
    }
}