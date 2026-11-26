using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CmSecurityService.Vehicles;

/// <summary>
/// Registers the vehicle telemetry services with dependency injection.
/// </summary>
public static class VehicleServiceCollectionExtensions
{
    /// <summary>
    /// Adds the EF Core telemetry service used by <c>VehiclesController</c>.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddVehicleTelemetry(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IVehicleTelemetryService, EfVehicleTelemetryService>();
        return services;
    }
}
