namespace CmSecurityService.Vehicles;

/// <summary>
/// Represents one vehicle telemetry record.
/// </summary>
public class VehicleTelemetryDto
{
    public Guid VehicleId { get; set; }

    public Address Address { get; set; } = new();

    public decimal? SpeedKmh { get; set; }

    public decimal? HeadingDegrees { get; set; }

    public DateTime RecordedAt { get; set; }

    public DateTime ReceivedAt { get; set; }
}