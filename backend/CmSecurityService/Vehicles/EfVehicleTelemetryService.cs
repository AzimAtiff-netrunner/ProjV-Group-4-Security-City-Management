using System.ComponentModel.DataAnnotations;
using CmSecurityService.Data;
using CmSecurityService.Incidents;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Vehicles;

/// <summary>
/// <see cref="IVehicleTelemetryService"/> backed by the MySQL database through EF Core.
/// </summary>
public class EfVehicleTelemetryService : IVehicleTelemetryService
{
    /// <summary>How far ahead of the server clock a recordedAt may be (allows for device clock drift).</summary>
    public static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="db">Database context.</param>
    /// <param name="clock">Source of the current time (replaceable in tests).</param>
    public EfVehicleTelemetryService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<PagedResult<VehicleDto>> GetVehiclesAsync(
        VehicleStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _db.Vehicles.AsNoTracking().Where(v => v.IsActive);
        if (status.HasValue)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var vehicles = await query
            .OrderBy(v => v.Callsign)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Newest recorded_at per vehicle on this page, then the matching rows.
        // Both steps use the (vehicle_id, recorded_at) index; the unique index guarantees one row per pair.
        var ids = vehicles.Select(v => v.VehicleId).ToList();
        var newestTimes = _db.VehicleTelemetryRecords
            .Where(t => ids.Contains(t.VehicleId))
            .GroupBy(t => t.VehicleId)
            .Select(g => new { VehicleId = g.Key, RecordedAt = g.Max(t => t.RecordedAt) });

        var latest = await (
            from t in _db.VehicleTelemetryRecords.AsNoTracking()
            join n in newestTimes
                on new { t.VehicleId, t.RecordedAt } equals new { n.VehicleId, n.RecordedAt }
            select new { t.VehicleId, t.StreetNumber, t.StreetName, t.City, t.PostalCode, t.RecordedAt})
            .ToListAsync(cancellationToken);

        var latestByVehicle = latest.ToDictionary(x => x.VehicleId);

        var items = vehicles.Select(v =>
        {
            VehicleLocationDto? location = null;
            if (latestByVehicle.TryGetValue(v.VehicleId, out var l))
            {
                location = new VehicleLocationDto
                {
                    Address = new Address
                    {
                        StreetNumber = l.StreetNumber,
                        StreetName = l.StreetName,
                        City = l.City,
                        PostalCode = l.PostalCode
                    },
                        RecordedAt = AsUtc(l.RecordedAt)
                };
            }

            return new VehicleDto
            {
                Id = v.VehicleId,
                Callsign = v.Callsign,
                LicensePlate = v.LicensePlate,
                Status = v.Status.ToString(),
                CurrentLocation = location
            };
        }).ToList();

        return new PagedResult<VehicleDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    /// <inheritdoc />
    public Task<bool> VehicleExistsAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        _db.Vehicles.AsNoTracking().AnyAsync(v => v.VehicleId == vehicleId && v.IsActive, cancellationToken);

    /// <inheritdoc />
    public async Task<VehicleTelemetryDto?> GetLatestTelemetryAsync(
        Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var row = await _db.VehicleTelemetryRecords
            .AsNoTracking()
            .Where(t => t.VehicleId == vehicleId)
            .OrderByDescending(t => t.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToDto(row);
    }

    /// <inheritdoc />
    public async Task<VehicleTelemetryDto> AddTelemetryAsync(
        Guid vehicleId, CreateVehicleTelemetryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Address is null || request.RecordedAt is null)
        {
            throw new ValidationException("address and recordedAt are required.");
        }

        var now = TruncateToMicroseconds(_clock.GetUtcNow().UtcDateTime);
        var recordedAt = TruncateToMicroseconds(NormalizeToUtc(request.RecordedAt.Value));

        if (recordedAt > now + FutureTolerance)
        {
            throw new ValidationException(
                $"recordedAt cannot be more than {(int)FutureTolerance.TotalMinutes} minutes in the future.");
        }

        if (await RecordExistsAsync(vehicleId, recordedAt, cancellationToken))
        {
            throw new DuplicateTelemetryException(vehicleId, recordedAt);
        }

        var row = new VehicleTelemetry
        {
            VehicleId = vehicleId,

            StreetNumber = request.Address.StreetNumber.Trim(),
            StreetName = request.Address.StreetName.Trim(),
            City = request.Address.City.Trim(),
            PostalCode = request.Address.PostalCode.Trim().ToUpperInvariant(),

            SpeedKmh = request.SpeedKmh.HasValue
                ? Math.Round(request.SpeedKmh.Value, 1)
                : null,

            HeadingDegrees = request.HeadingDegrees.HasValue
                ? Math.Round(request.HeadingDegrees.Value, 2)
                : null,

            RecordedAt = recordedAt,
            ReceivedAt = now
        };

        _db.VehicleTelemetryRecords.Add(row);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two identical updates arriving at once: the unique index rejects the second one.
            _db.Entry(row).State = EntityState.Detached;
            if (await RecordExistsAsync(vehicleId, recordedAt, cancellationToken))
            {
                throw new DuplicateTelemetryException(vehicleId, recordedAt);
            }

            throw;
        }

        return ToDto(row);
    }

    /// <inheritdoc />
    public async Task<VehicleDto?> GetVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles.AsNoTracking()
            .FirstOrDefaultAsync(v => v.VehicleId == vehicleId && v.IsActive, cancellationToken);
        if (vehicle is null)
        {
            return null;
        }

        var latest = await _db.VehicleTelemetryRecords.AsNoTracking()
            .Where(t => t.VehicleId == vehicleId)
            .OrderByDescending(t => t.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        VehicleLocationDto? location = latest is null
        ? null
        : new VehicleLocationDto
        {
            Address = new Address
            {
                StreetNumber = latest.StreetNumber,
                StreetName = latest.StreetName,
                City = latest.City,
                PostalCode = latest.PostalCode
            },
            RecordedAt = AsUtc(latest.RecordedAt)
        };

        return ToVehicleDto(vehicle, location);
    }

    /// <inheritdoc />
    public async Task<VehicleDto> AddVehicleAsync(
        string callsign, string licensePlate, VehicleStatus status, CancellationToken cancellationToken = default)
    {
        callsign = callsign.Trim();
        licensePlate = licensePlate.Trim();
        if (callsign.Length == 0 || licensePlate.Length == 0)
        {
            throw new ValidationException("callsign and licensePlate cannot be blank.");
        }

        await ThrowIfVehicleDuplicateAsync(callsign, licensePlate, cancellationToken);

        var vehicle = new Vehicle
        {
            VehicleId = Guid.NewGuid(),
            Callsign = callsign,
            LicensePlate = licensePlate,
            Status = status,
            IsActive = true,
            CreatedAt = TruncateToMicroseconds(_clock.GetUtcNow().UtcDateTime)
        };

        _db.Vehicles.Add(vehicle);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two identical requests at once: the unique indexes reject the second one.
            _db.Entry(vehicle).State = EntityState.Detached;
            await ThrowIfVehicleDuplicateAsync(callsign, licensePlate, cancellationToken);
            throw;
        }

        return ToVehicleDto(vehicle, null);
    }

    public async Task<VehicleDto?> UpdateVehicleAsync(Guid vehicleId, string callsign, string licensePlate, VehicleStatus status, CancellationToken cancellationToken = default)
    {
        callsign = callsign.Trim();
        licensePlate = licensePlate.Trim();
        if (string.IsNullOrWhiteSpace(callsign) || string.IsNullOrWhiteSpace(licensePlate))
            throw new ValidationException("callsign and licensePlate cannot be blank.");

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId && v.IsActive, cancellationToken);
        if (vehicle is null) return null;
        if (await _db.Vehicles.AnyAsync(v => v.VehicleId != vehicleId && v.Callsign == callsign, cancellationToken))
            throw new DuplicateVehicleException("callsign", callsign);
        if (await _db.Vehicles.AnyAsync(v => v.VehicleId != vehicleId && v.LicensePlate == licensePlate, cancellationToken))
            throw new DuplicateVehicleException("licensePlate", licensePlate);

        vehicle.Callsign = callsign;
        vehicle.LicensePlate = licensePlate;
        vehicle.Status = status;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetVehicleAsync(vehicleId, cancellationToken);
    }

    public async Task<bool> RetireVehicleAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId && v.IsActive, cancellationToken);
        if (vehicle is null) return false;
        vehicle.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>Checks every vehicle, retired ones included, because the database unique indexes do too.</summary>
    private async Task ThrowIfVehicleDuplicateAsync(string callsign, string licensePlate, CancellationToken cancellationToken)
    {
        if (await _db.Vehicles.AsNoTracking().AnyAsync(v => v.Callsign == callsign, cancellationToken))
        {
            throw new DuplicateVehicleException("callsign", callsign);
        }

        if (await _db.Vehicles.AsNoTracking().AnyAsync(v => v.LicensePlate == licensePlate, cancellationToken))
        {
            throw new DuplicateVehicleException("licensePlate", licensePlate);
        }
    }

    private static VehicleDto ToVehicleDto(Vehicle vehicle, VehicleLocationDto? location) => new()
    {
        Id = vehicle.VehicleId,
        Callsign = vehicle.Callsign,
        LicensePlate = vehicle.LicensePlate,
        Status = vehicle.Status.ToString(),
        CurrentLocation = location
    };

    private Task<bool> RecordExistsAsync(Guid vehicleId, DateTime recordedAt, CancellationToken cancellationToken) =>
        _db.VehicleTelemetryRecords.AsNoTracking()
            .AnyAsync(t => t.VehicleId == vehicleId && t.RecordedAt == recordedAt, cancellationToken);

    private static VehicleTelemetryDto ToDto(VehicleTelemetry row) => new()
    {
        VehicleId = row.VehicleId,
        Address = row.Address,
        SpeedKmh = row.SpeedKmh,
        HeadingDegrees = row.HeadingDegrees,
        RecordedAt = AsUtc(row.RecordedAt),
        ReceivedAt = AsUtc(row.ReceivedAt)
    };

    /// <summary>The database returns DateTimes with no kind; mark them UTC so JSON gets a trailing Z.</summary>
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    /// <summary>Converts an incoming timestamp to UTC. A value with no offset is taken as already UTC.</summary>
    private static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>MySQL DATETIME(6) keeps microseconds; .NET keeps 100 ns ticks. Cut to match the column.</summary>
    private static DateTime TruncateToMicroseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Utc);
}
