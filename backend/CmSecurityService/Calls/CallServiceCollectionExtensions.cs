namespace CmSecurityService.Calls;

/// <summary>
/// Registers the View 9-1-1 Calls services with dependency injection.
/// </summary>
public static class CallServiceCollectionExtensions
{
    /// <summary>
    /// Adds the call query service used by <c>CallsController</c>.
    /// </summary>
    /// <remarks>
    /// Uses seed data until the emergency_call table exists; then replace the factory
    /// below with the EF Core implementation. A factory is used on purpose: the DI container
    /// would otherwise pick the constructor that takes a list and inject an empty list.
    /// </remarks>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddCallViewing(this IServiceCollection services)
    {
        services.AddSingleton<ICallQueryService>(_ => new InMemoryCallQueryService());
        return services;
    }
}
