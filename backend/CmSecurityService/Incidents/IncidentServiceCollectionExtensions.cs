   namespace CmSecurityService.Incidents;

   /// <summary>
   /// Registers the View Incidents (#216) services with dependency injection.
   /// </summary>
   public static class IncidentServiceCollectionExtensions
   {
       /// <summary>
       /// Adds the EF Core incident query service used by <c>IncidentsController</c>.
       /// </summary>
       public static IServiceCollection AddIncidentViewing(this IServiceCollection services)
       {
           services.AddScoped<IIncidentQueryService, EfIncidentQueryService>();
           return services;
       }
   }
