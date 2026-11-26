using System.ComponentModel.DataAnnotations.Schema;

namespace CmSecurityService.Vehicles;


/// Stores one location update from a vehicle.

public class VehicleTelemetry
{
    public long TelemetryId { get; set; }

    public Guid VehicleId { get; set; }

    public string StreetNumber { get; set; } = string.Empty;

    public string StreetName { get; set; } = string.Empty;

    public string City { get; set; } = "Waterloo";

    public string PostalCode { get; set; } = string.Empty;

    public decimal? SpeedKmh { get; set; }

    public decimal? HeadingDegrees { get; set; }

    public DateTime RecordedAt { get; set; }

    public DateTime ReceivedAt { get; set; }

    public Vehicle? Vehicle { get; set; }

    /// Gets the shared address representation.
    [NotMapped]
    public Address Address => new()
    {
        StreetNumber = StreetNumber,
        StreetName = StreetName,
        City = City,
        PostalCode = PostalCode
    };
}