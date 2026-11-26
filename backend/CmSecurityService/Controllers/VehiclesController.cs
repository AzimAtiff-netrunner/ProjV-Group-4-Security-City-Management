using System.ComponentModel.DataAnnotations;
using CmSecurityService.Authorization;
using CmSecurityService.Common;
using CmSecurityService.Incidents;
using CmSecurityService.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Controllers;

/// <summary>
/// Police vehicles and their location telemetry.
/// </summary>
/// <remarks>
/// Every endpoint requires a valid JWT from an active officer (policy <see cref="TelemetryPolicies.Viewer"/>).
/// Recording telemetry additionally requires <see cref="TelemetryPolicies.Writer"/>.
/// </remarks>
[ApiController]
[Route("api/security/v1/vehicles")]
[Produces("application/json")]
[Authorize(Policy = TelemetryPolicies.Viewer)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleTelemetryService _vehicles;

    /// <summary>
    /// Creates the controller.
    /// </summary>
    /// <param name="vehicles">Source of vehicle and telemetry data.</param>
    public VehiclesController(IVehicleTelemetryService vehicles)
    {
        _vehicles = vehicles;
    }

    /// <summary>
    /// Lists police vehicles with their current location.
    /// </summary>
    /// <param name="query">Filters: status (Available, OnPatrol, Dispatched, OutOfService), page (default 1), pageSize (default 20, max 100).</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <response code="200">A page of vehicles. currentLocation is null for a vehicle that has never reported.</response>
    /// <response code="400">One or more query values are invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VehicleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<VehicleDto>>> GetVehicles(
        [FromQuery] VehicleQuery query,
        CancellationToken cancellationToken)
    {
        var errors = query.Validate(out var status);
        if (errors.Count > 0)
        {
            return BadRequest(ApiError.Create(HttpContext, "invalid_query", string.Join(" ", errors)));
        }

        var result = await _vehicles.GetVehiclesAsync(status, query.Page, query.PageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets the latest telemetry record for one vehicle.
    /// </summary>
    /// <param name="vehicleId">Vehicle identifier (GUID).</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <response code="200">The record with the newest recordedAt.</response>
    /// <response code="400">vehicleId is not a valid GUID.</response>
    /// <response code="404">No such vehicle (vehicle_not_found), or it has never reported (telemetry_not_found).</response>
    [HttpGet("{vehicleId}/telemetry/latest")]
    [ProducesResponseType(typeof(VehicleTelemetryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleTelemetryDto>> GetLatestTelemetry(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        if (!await _vehicles.VehicleExistsAsync(vehicleId, cancellationToken))
        {
            return VehicleNotFound(vehicleId);
        }

        var latest = await _vehicles.GetLatestTelemetryAsync(vehicleId, cancellationToken);
        if (latest is null)
        {
            return NotFound(ApiError.Create(
                HttpContext, "telemetry_not_found", $"Vehicle {vehicleId} has not reported any telemetry yet."));
        }

        return Ok(latest);
    }

    /// <summary>
    /// Records a location update for a vehicle.
    /// </summary>
    /// <param name="vehicleId">Vehicle identifier (GUID).</param>
    /// <param name="request">Address and recordedAt (UTC); optional speedKmh and headingDegrees.</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <response code="201">The stored record.</response>
    /// <response code="400">Invalid body, or recordedAt is more than 5 minutes in the future.</response>
    /// <response code="404">No such vehicle.</response>
    /// <response code="409">An update for this vehicle and recordedAt already exists.</response>
    [HttpPost("{vehicleId}/telemetry")]
    [Authorize(Policy = TelemetryPolicies.Writer)]
    [ProducesResponseType(typeof(VehicleTelemetryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleTelemetryDto>> RecordTelemetry(
        Guid vehicleId,
        [FromBody] CreateVehicleTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        // Field rules (required, ranges) are checked by [ApiController] before this runs.
        if (!await _vehicles.VehicleExistsAsync(vehicleId, cancellationToken))
        {
            return VehicleNotFound(vehicleId);
        }

        try
        {
            var created = await _vehicles.AddTelemetryAsync(vehicleId, request, cancellationToken);
            return CreatedAtAction(nameof(GetLatestTelemetry), new { vehicleId }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiError.Create(HttpContext, "validation_failed", ex.Message));
        }
        catch (DuplicateTelemetryException ex)
        {
            return Conflict(ApiError.Create(HttpContext, "duplicate_telemetry", ex.Message));
        }
    }

    /// <summary>
    /// Gets one vehicle with its current location.
    /// </summary>
    /// <param name="vehicleId">Vehicle identifier (GUID).</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <response code="200">The vehicle.</response>
    /// <response code="400">vehicleId is not a valid GUID.</response>
    /// <response code="404">No such vehicle.</response>
    [HttpGet("{vehicleId}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleDto>> GetVehicle(Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicles.GetVehicleAsync(vehicleId, cancellationToken);
        if (vehicle is null)
        {
            return VehicleNotFound(vehicleId);
        }

        return Ok(vehicle);
    }

    /// <summary>
    /// Registers a new vehicle (Security Administrators only).
    /// </summary>
    /// <param name="request">callsign and licensePlate (both unique); optional status, default Available.</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <response code="201">The new vehicle with its generated id. Its currentLocation is null until it reports one.</response>
    /// <response code="400">Invalid body, or status is not one of Available, OnPatrol, Dispatched, OutOfService.</response>
    /// <response code="409">Another vehicle already has this callsign or licensePlate.</response>
    [HttpPost]
    [Authorize(Policy = TelemetryPolicies.Manager)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VehicleDto>> CreateVehicle(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        // Required fields and lengths are checked by [ApiController] before this runs.
        if (!request.TryGetStatus(out var status))
        {
            return BadRequest(ApiError.Create(
                HttpContext,
                "invalid_request",
                $"status '{request.Status}' is not valid. Use one of: {string.Join(", ", Enum.GetNames<VehicleStatus>())}."));
        }

        try
        {
            var created = await _vehicles.AddVehicleAsync(
                request.Callsign!, request.LicensePlate!, status, cancellationToken);
            return CreatedAtAction(nameof(GetVehicle), new { vehicleId = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiError.Create(HttpContext, "validation_failed", ex.Message));
        }
        catch (DuplicateVehicleException ex)
        {
            return Conflict(ApiError.Create(HttpContext, "duplicate_vehicle", ex.Message));
        }
    }

    /// <summary>Updates a vehicle's callsign, plate and operational status.</summary>
    [HttpPut("{vehicleId}")]
    [Authorize(Policy = TelemetryPolicies.Manager)]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleDto>> UpdateVehicle(Guid vehicleId, [FromBody] UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        if (!request.TryGetStatus(out var status))
            return BadRequest(ApiError.Create(HttpContext, "invalid_request", "Use status Available, OnPatrol, Dispatched, or OutOfService."));
        try
        {
            var updated = await _vehicles.UpdateVehicleAsync(vehicleId, request.Callsign!, request.LicensePlate!, status, cancellationToken);
            return updated is null ? VehicleNotFound(vehicleId) : Ok(updated);
        }
        catch (DuplicateVehicleException ex)
        {
            return Conflict(ApiError.Create(HttpContext, "duplicate_vehicle", ex.Message));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiError.Create(HttpContext, "validation_failed", ex.Message));
        }
    }

    /// <summary>Retires a vehicle from the active fleet without deleting its telemetry history.</summary>
    [HttpDelete("{vehicleId}")]
    [Authorize(Policy = TelemetryPolicies.Manager)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteVehicle(Guid vehicleId, CancellationToken cancellationToken)
    {
        return await _vehicles.RetireVehicleAsync(vehicleId, cancellationToken)
            ? NoContent()
            : VehicleNotFound(vehicleId);
    }

    private NotFoundObjectResult VehicleNotFound(Guid vehicleId) =>
        NotFound(ApiError.Create(HttpContext, "vehicle_not_found", $"No vehicle exists with id {vehicleId}."));
}
