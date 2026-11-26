namespace CmSecurityService.Vehicles;

/// <summary>
/// Storage model for the <c>vehicle</c> table: one police vehicle.
/// </summary>
public class Vehicle
{
    /// <summary>Unique vehicle identifier exposed by the API.</summary>
    public Guid VehicleId { get; set; }

    /// <summary>Radio call sign, e.g. "UNIT-101". Unique.</summary>
    public string Callsign { get; set; } = string.Empty;

    /// <summary>Licence plate. Unique.</summary>
    public string LicensePlate { get; set; } = string.Empty;

    /// <summary>Current operational status.</summary>
    public VehicleStatus Status { get; set; } = VehicleStatus.Available;

    /// <summary>False when the vehicle has been retired from the fleet.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the vehicle was registered (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Location history for this vehicle.</summary>
    public ICollection<VehicleTelemetry> Telemetry { get; set; } = new List<VehicleTelemetry>();
}
