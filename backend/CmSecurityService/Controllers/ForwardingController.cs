using CmSecurityService.Forwarding;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Controllers;

[ApiController]
[Route("api/security/v1/calls")]
public class ForwardingController : ControllerBase
{
    private readonly IForwardingService _forwardingService;

    public ForwardingController(IForwardingService forwardingService)
    {
        _forwardingService = forwardingService;
    }

    [HttpGet("classify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ClassifyCall([FromQuery] string incidentType = "", [FromQuery] string description = "")
    {
        var needsForwarding = _forwardingService.RequiresEmergencyForwarding(
            incidentType, description, out var recommendedService);

        return Ok(new
        {
            requiresForwarding = needsForwarding,
            recommendedService
        });
    }

    [HttpPost("{id:int}/forward")]
    [ProducesResponseType(typeof(ForwardingRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ForwardingRecord), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForwardCall(
        int id,
        [FromBody] ForwardCallRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.CallId)
        {
            return BadRequest(new { message = "Route call ID must match request CallId." });
        }

        if (request.TargetService is not ("Fire" or "EMS"))
        {
            return BadRequest(new { message = "TargetService must be either 'Fire' or 'EMS'." });
        }

        var result = await _forwardingService.ForwardCallAsync(request, cancellationToken);

        if (result.Status == "Failed")
        {
            return StatusCode(StatusCodes.Status502BadGateway, result);
        }

        return Ok(result);
    }

    [HttpGet("forwarding-history")]
    [ProducesResponseType(typeof(IReadOnlyList<ForwardingRecord>), StatusCodes.Status200OK)]
    public IActionResult GetHistory([FromQuery] int? callId = null)
    {
        return Ok(_forwardingService.GetForwardingHistory(callId));
    }
}
