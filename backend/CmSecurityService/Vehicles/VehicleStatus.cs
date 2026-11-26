namespace CmSecurityService.Vehicles;

/// <summary>
/// Operational status of a police vehicle. Stored by name in <c>vehicle.status</c>.
/// </summary>
public enum VehicleStatus
{
    /// <summary>In service and free to be assigned.</summary>
    Available,

    /// <summary>Out on routine patrol.</summary>
    OnPatrol,

    /// <summary>Assigned to an incident or 9-1-1 call.</summary>
    Dispatched,

    /// <summary>Not in service (maintenance, damaged, etc.).</summary>
    OutOfService
}
