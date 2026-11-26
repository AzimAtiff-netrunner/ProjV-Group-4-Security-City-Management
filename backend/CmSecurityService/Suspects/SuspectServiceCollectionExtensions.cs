namespace CmSecurityService.Suspects;

/// <summary>
/// Registers the suspect records and crime rankings services (story #225).
/// </summary>
public static class SuspectServiceCollectionExtensions
{
    /// <summary>
    /// Adds the EF Core suspect service used by <c>SuspectsController</c> and <c>NeighborhoodsController</c>.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddSuspectViewing(this IServiceCollection services)
    {
        services.AddScoped<ISuspectQueryService, EfSuspectQueryService>();
        return services;
    }
}
