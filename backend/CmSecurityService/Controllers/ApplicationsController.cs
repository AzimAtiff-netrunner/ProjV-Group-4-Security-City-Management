using CmSecurityService.Applications;
using CmSecurityService.Common;
using CmSecurityService.Incidents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Controllers;

/// <summary>
/// Security Administrator endpoints for reviewing police applications (User Story #222).
/// </summary>
[ApiController]
[Route("api/security/v1/applications")]
[Authorize(Policy = "SecurityAdministrator")]
public class ApplicationsController : ControllerBase
{
    private readonly IPoliceApplicationService _applications;

    public ApplicationsController(IPoliceApplicationService applications)
    {
        _applications = applications;
    }

    /// <summary>
    /// Retrieves police applications (defaults to Pending applications, AC 1 &amp; 2).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PoliceApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetApplications(
        [FromQuery] ApplicationQuery query,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate(out var status, out _);
        if (errors.Count > 0)
        {
            return BadRequest(ApiError.Create(HttpContext, "invalid_query", string.Join(" ", errors)));
        }

        var result = await _applications.GetApplicationsAsync(
            status, query.Page, query.PageSize, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single police application by ID (AC 2, 3 &amp; 9).
    /// </summary>
    [HttpGet("{applicationId:guid}")]
    [ProducesResponseType(typeof(PoliceApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetApplicationById(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await _applications.GetApplicationByIdAsync(applicationId, cancellationToken);
        if (application is null)
        {
            return NotFound(ApiError.Create(
                HttpContext,
                "application_not_found",
                $"No police application exists with id '{applicationId}'."));
        }

        return Ok(application);
    }

    /// <summary>
    /// Approves a pending police application (AC 4, 6, 7, 8 &amp; 9).
    /// </summary>
    [HttpPut("{applicationId:guid}/approve")]
    [ProducesResponseType(typeof(PoliceApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveApplication(
        Guid applicationId,
        [FromBody] ReviewApplicationRequest? request,
        CancellationToken cancellationToken)
    {
        var reviewerUserId = User.FindFirst("sub")?.Value;

        try
        {
            var updated = await _applications.ApproveApplicationAsync(
                applicationId,
                reviewerUserId,
                request?.Notes,
                cancellationToken);

            if (updated is null)
            {
                return NotFound(ApiError.Create(
                    HttpContext,
                    "application_not_found",
                    $"No police application exists with id '{applicationId}'."));
            }

            return Ok(updated);
        }
        catch (ApplicationAlreadyFinalizedException ex)
        {
            return Conflict(ApiError.Create(
                HttpContext,
                "application_already_finalized",
                ex.Message));
        }
    }

    /// <summary>
    /// Rejects a pending police application (AC 5, 6, 7, 8 &amp; 9).
    /// </summary>
    [HttpPut("{applicationId:guid}/reject")]
    [ProducesResponseType(typeof(PoliceApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectApplication(
        Guid applicationId,
        [FromBody] ReviewApplicationRequest? request,
        CancellationToken cancellationToken)
    {
        var reviewerUserId = User.FindFirst("sub")?.Value;

        try
        {
            var updated = await _applications.RejectApplicationAsync(
                applicationId,
                reviewerUserId,
                request?.Notes,
                cancellationToken);

            if (updated is null)
            {
                return NotFound(ApiError.Create(
                    HttpContext,
                    "application_not_found",
                    $"No police application exists with id '{applicationId}'."));
            }

            return Ok(updated);
        }
        catch (ApplicationAlreadyFinalizedException ex)
        {
            return Conflict(ApiError.Create(
                HttpContext,
                "application_already_finalized",
                ex.Message));
        }
    }
}