using System.ComponentModel.DataAnnotations;
using CmSecurityService.Data;
using CmSecurityService.Vehicles;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Tests;

public sealed class VehicleTelemetryServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 18, 0, 0, DateTimeKind.Utc);

    private static (AppDbContext Db, EfVehicleTelemetryService Service) CreateService()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"vehicle-tests-{Guid.NewGuid()}")
            .Options;
        var db = new AppDbContext(options);
        return (db, new EfVehicleTelemetryService(db, new FrozenTimeProvider(Now)));
    }

    private static async Task<Guid> AddVehicle(EfVehicleTelemetryService service) =>
        (await service.AddVehicleAsync("UNIT-101", "TEST 101", VehicleStatus.Available)).Id;

    private static CreateVehicleTelemetryRequest Request(DateTime? recordedAt = null) => new(
        new Address { StreetNumber = "100", StreetName = "King Street", City = "Waterloo", PostalCode = "N2J 2W9" },
        recordedAt ?? Now.AddMinutes(-1), 62.4m, 180.25m);

    [Fact]
    public async Task AddTelemetryAsync_PersistsLocationSpeedAndHeading()
    {
        var (db, service) = CreateService();
        await using var _ = db;
        var vehicleId = await AddVehicle(service);

        var result = await service.AddTelemetryAsync(vehicleId, Request());
        var stored = await service.GetLatestTelemetryAsync(vehicleId);

        Assert.NotNull(stored);
        Assert.Equal(vehicleId, result.VehicleId);
        Assert.Equal("King Street", stored.Address.StreetName);
        Assert.Equal("N2J 2W9", stored.Address.PostalCode);
        Assert.Equal(62.4m, stored.SpeedKmh);
        Assert.Equal(180.25m, stored.HeadingDegrees);
        Assert.Equal(Now.AddMinutes(-1), stored.RecordedAt);
        Assert.Single(await db.VehicleTelemetryRecords.ToListAsync());
    }

    [Fact]
    public async Task AddTelemetryAsync_RejectsDuplicateVehicleTimestamp()
    {
        var (db, service) = CreateService();
        await using var _ = db;
        var vehicleId = await AddVehicle(service);
        var request = Request();
        await service.AddTelemetryAsync(vehicleId, request);

        await Assert.ThrowsAsync<DuplicateTelemetryException>(() => service.AddTelemetryAsync(vehicleId, request));
        Assert.Single(await db.VehicleTelemetryRecords.ToListAsync());
    }

    [Fact]
    public async Task AddTelemetryAsync_RejectsTimestampTooFarInFuture()
    {
        var (db, service) = CreateService();
        await using var _ = db;
        var vehicleId = await AddVehicle(service);

        await Assert.ThrowsAsync<ValidationException>(() => service.AddTelemetryAsync(vehicleId, Request(Now.AddMinutes(6))));
        Assert.Empty(await db.VehicleTelemetryRecords.ToListAsync());
    }

    [Fact]
    public async Task GetLatestTelemetryAsync_ReturnsNewestReport()
    {
        var (db, service) = CreateService();
        await using var _ = db;
        var vehicleId = await AddVehicle(service);
        await service.AddTelemetryAsync(vehicleId, Request(Now.AddMinutes(-2)));
        var newestRequest = Request(Now.AddMinutes(-1)) with
        {
            Address = new Address { StreetNumber = "200", StreetName = "Weber Street", City = "Waterloo", PostalCode = "N2J 3H8" },
            SpeedKmh = 80m,
            HeadingDegrees = 90m
        };
        await service.AddTelemetryAsync(vehicleId, newestRequest);

        var latest = await service.GetLatestTelemetryAsync(vehicleId);

        Assert.NotNull(latest);
        Assert.Equal("Weber Street", latest.Address.StreetName);
        Assert.Equal(80m, latest.SpeedKmh);
        Assert.Equal(90m, latest.HeadingDegrees);
    }

    [Fact]
    public async Task RetireVehicleAsync_HidesVehicleButPreservesTelemetryHistory()
    {
        var (db, service) = CreateService();
        await using var _ = db;
        var vehicleId = await AddVehicle(service);
        await service.AddTelemetryAsync(vehicleId, Request());

        Assert.True(await service.RetireVehicleAsync(vehicleId));

        Assert.False(await service.VehicleExistsAsync(vehicleId));
        Assert.Null(await service.GetVehicleAsync(vehicleId));
        Assert.Single(await db.VehicleTelemetryRecords.ToListAsync());
        Assert.False(await service.RetireVehicleAsync(vehicleId));
    }

    private sealed class FrozenTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
    }
}
