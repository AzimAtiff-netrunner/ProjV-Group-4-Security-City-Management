using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CmSecurityService.Applications;

/// <summary>
/// Registers police application review services with dependency injection.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddPoliceApplications(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPoliceApplicationService, EfPoliceApplicationService>();
        return services;
    }
}