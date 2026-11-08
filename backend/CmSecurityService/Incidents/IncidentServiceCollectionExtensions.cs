namespace CmSecurityService.Incidents;

/// <summary>
/// Registers the View Incidents (#216) services with dependency injection.
/// </summary>
public static class IncidentServiceCollectionExtensions
{
    /// <summary>
    /// Adds the incident query service used by <c>IncidentsController</c>.
    /// </summary>
    /// <remarks>
    /// Uses seed data until the Incident database (#215/#217) is merged; then
    /// replace the factory below with the EF Core implementation.
    /// A factory is used on purpose: <see cref="InMemoryIncidentQueryService"/> has a
    /// second constructor taking a list of records, and the DI container would pick it
    /// and inject an empty list, so the API would return no incidents.
    /// </remarks>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddIncidentViewing(this IServiceCollection services)
    {
        services.AddSingleton<IIncidentQueryService>(_ => new InMemoryIncidentQueryService());
        return services;
    }
}
