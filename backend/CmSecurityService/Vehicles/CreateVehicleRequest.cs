using System.ComponentModel.DataAnnotations;

namespace CmSecurityService.Vehicles;

/// <summary>
/// Body of <c>POST /api/security/v1/vehicles</c>.
/// </summary>
/// <remarks>
/// Callsign and licensePlate are required. Status is optional and defaults to Available.
/// Callsign and licensePlate must each be unique across all vehicles (checked by the service).
/// </remarks>
public record CreateVehicleRequest(
    [Required, StringLength(30, MinimumLength = 1)] string? Callsign,
    [Required, StringLength(20, MinimumLength = 1)] string? LicensePlate,
    [StringLength(30)] string? Status)
{
    /// <summary>
    /// Reads <see cref="Status"/> by name (case-insensitive). A missing status means Available.
    /// </summary>
    /// <param name="status">The parsed status.</param>
    /// <returns>False if a status was given but is not one of the allowed names.</returns>
    public bool TryGetStatus(out VehicleStatus status)
    {
        status = VehicleStatus.Available;
        if (string.IsNullOrWhiteSpace(Status))
        {
            return true;
        }

        var trimmed = Status.Trim();
        return Enum.TryParse(trimmed, ignoreCase: true, out status)
               && Enum.IsDefined(status)
               && !int.TryParse(trimmed, out _);
    }
}
