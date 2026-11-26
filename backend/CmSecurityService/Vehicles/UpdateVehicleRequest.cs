using System.ComponentModel.DataAnnotations;

namespace CmSecurityService.Vehicles;

public record UpdateVehicleRequest(
    [Required, StringLength(30, MinimumLength = 1)] string? Callsign,
    [Required, StringLength(20, MinimumLength = 1)] string? LicensePlate,
    [Required, StringLength(30)] string? Status)
{
    public bool TryGetStatus(out VehicleStatus status)
    {
        status = VehicleStatus.Available;
        var value = Status?.Trim();
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse(value, true, out status)
            && Enum.IsDefined(status)
            && !int.TryParse(value, out _);
    }
}
