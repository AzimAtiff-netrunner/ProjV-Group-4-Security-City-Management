using System.ComponentModel.DataAnnotations;

namespace CmSecurityService.Vehicles;

/// <summary>
/// Body of <c>POST /api/security/v1/vehicles/{vehicleId}/telemetry</c>.
/// </summary>
/// <remarks>
/// Latitude, longitude and recordedAt are nullable only so that a missing field is reported as an
/// error instead of silently becoming 0 or the default date. Range and required checks run
/// automatically through <c>[ApiController]</c>. Rules that need the clock or the database
/// (timestamp not in the future, duplicate update) are enforced by the telemetry service.
/// </remarks>
public record CreateVehicleTelemetryRequest(
    [Required] Address? Address,
    [Required] DateTime? RecordedAt,
    [Range(0.0, 500.0)] decimal? SpeedKmh,
    [Range(0.0, 359.99)] decimal? HeadingDegrees
);
