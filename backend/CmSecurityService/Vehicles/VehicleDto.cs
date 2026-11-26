namespace CmSecurityService.Vehicles;

/// <summary>
/// Public representation of a police vehicle.
/// </summary>
public class VehicleDto
{
    public Guid Id { get; set; }

    public string Callsign { get; set; } = string.Empty;

    public string LicensePlate { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public VehicleLocationDto? CurrentLocation { get; set; }
}

/// <summary>
/// Current vehicle address and recorded timestamp.
/// </summary>
public class VehicleLocationDto
{
    public Address Address { get; set; } = new();

    public DateTime RecordedAt { get; set; }
}