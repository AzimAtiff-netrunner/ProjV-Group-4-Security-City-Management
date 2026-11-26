using System.ComponentModel.DataAnnotations;
using CmSecurityService.Incidents;

namespace CmSecurityService.Vehicles;

/// <summary>
/// Reads vehicles and records and retrieves their telemetry.
/// </summary>
/// <remarks>
/// The controller depends on this interface, not on the database.
/// Only active vehicles are visible: a retired vehicle is treated as not found.
/// </remarks>
public interface IVehicleTelemetryService
{
    /// <summary>
    /// Returns one page of active vehicles, ordered by call sign, each with its latest known location.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<PagedResult<VehicleDto>> GetVehiclesAsync(
        VehicleStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells whether an active vehicle with this id exists.
    /// </summary>
    /// <param name="vehicleId">Vehicle identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<bool> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the telemetry record with the newest <c>recordedAt</c> for a vehicle.
    /// </summary>
    /// <param name="vehicleId">Vehicle identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The record, or null if the vehicle has never reported.</returns>
    Task<VehicleTelemetryDto?> GetLatestTelemetryAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a new location update for a vehicle that exists.
    /// </summary>
    /// <param name="vehicleId">Vehicle identifier.</param>
    /// <param name="request">The update. Address and recordedAt must be present.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The stored record.</returns>
    /// <exception cref="ValidationException">recordedAt is too far in the future.</exception>
    /// <exception cref="DuplicateTelemetryException">An update for this vehicle and time already exists.</exception>
    Task<VehicleTelemetryDto> AddTelemetryAsync(
        Guid vehicleId, CreateVehicleTelemetryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one active vehicle with its latest known location.
    /// </summary>
    /// <param name="vehicleId">Vehicle identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The vehicle, or null if there is no active vehicle with that id.</returns>
    Task<VehicleDto?> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new vehicle. It starts with no location until it reports one.
    /// </summary>
    /// <param name="callsign">Call sign, e.g. "UNIT-104". Must be unique.</param>
    /// <param name="licensePlate">Licence plate. Must be unique.</param>
    /// <param name="status">Initial status.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The new vehicle, including its generated id.</returns>
    /// <exception cref="ValidationException">Callsign or licensePlate is blank.</exception>
    /// <exception cref="DuplicateVehicleException">Another vehicle (even a retired one) already has this call sign or plate.</exception>
    Task<VehicleDto> AddVehicleAsync(
        string callsign, string licensePlate, VehicleStatus status, CancellationToken cancellationToken = default);

    Task<VehicleDto?> UpdateVehicleAsync(Guid vehicleId, string callsign, string licensePlate, VehicleStatus status, CancellationToken cancellationToken = default);

    Task<bool> RetireVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Thrown when a vehicle with the same call sign or licence plate already exists.
/// </summary>
public class DuplicateVehicleException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="field">Which field clashed, e.g. "callsign".</param>
    /// <param name="value">The value that is already taken.</param>
    public DuplicateVehicleException(string field, string value)
        : base($"A vehicle with {field} '{value}' already exists.")
    {
    }
}

/// <summary>
/// Thrown when a telemetry update for the same vehicle and <c>recordedAt</c> is already stored.
/// </summary>
public class DuplicateTelemetryException : Exception
{
    /// <summary>Creates the exception.</summary>
    /// <param name="vehicleId">Vehicle the update was for.</param>
    /// <param name="recordedAt">The duplicated timestamp (UTC).</param>
    public DuplicateTelemetryException(Guid vehicleId, DateTime recordedAt)
        : base($"Telemetry for vehicle {vehicleId} at {recordedAt:O} has already been recorded.")
    {
    }
}
