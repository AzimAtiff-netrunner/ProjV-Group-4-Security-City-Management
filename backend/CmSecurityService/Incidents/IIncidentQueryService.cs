namespace CmSecurityService.Incidents;

/// <summary>
/// Read-only access to police incidents.
/// </summary>
/// <remarks>
/// The controller depends on this interface, not on a concrete data source.
/// Today it is backed by <see cref="InMemoryIncidentQueryService"/> (seed data).
/// When the Incident tables (#215) and Register Incidents (#217) are merged,
/// add an EF Core implementation and swap the registration in Program.cs;
/// the controller and tests do not need to change.
/// </remarks>
public interface IIncidentQueryService
{
    /// <summary>
    /// Returns one page of incidents matching the filters, newest first.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="type">Optional incident type filter (case-insensitive).</param>
    /// <param name="from">Optional start of reported date range (inclusive).</param>
    /// <param name="to">Optional end of reported date range (inclusive).</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A page of incidents and the total match count.</returns>
    Task<PagedResult<IncidentDto>> GetIncidentsAsync(
        IncidentStatus? status,
        string? type,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single incident by id.
    /// </summary>
    /// <param name="id">Incident identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The incident, or null if no incident has that id.</returns>
    Task<IncidentDto?> GetIncidentByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IncidentDto> AddIncidentAsync(string title, string description, string incidentType, string status, string location);
}
