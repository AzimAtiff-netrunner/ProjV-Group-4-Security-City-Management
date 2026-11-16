namespace CmSecurityService.Incidents;

/// <summary>
/// Access to police incidents: viewing (#216) and registering (#217).
/// </summary>
/// <remarks>
/// The controller depends on this interface, not on a concrete data source.
/// It is implemented by <see cref="EfIncidentQueryService"/>, which reads from and
/// writes to the database.
/// </remarks>
public interface IIncidentQueryService
{
    /// <summary>
    /// Returns one page of incidents matching the filters, newest first.
    /// </summary>
    /// <param name="status">Optional status name filter (case-insensitive). Must match a status in the database.</param>
    /// <param name="type">Optional incident type filter (case-insensitive).</param>
    /// <param name="from">Optional start of reported date range (inclusive).</param>
    /// <param name="to">Optional end of reported date range (inclusive).</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A page of incidents and the total match count.</returns>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
    /// The status filter does not match a known status.
    /// </exception>
    Task<PagedResult<IncidentDto>> GetIncidentsAsync(
        string? status,
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
    Task<IncidentDto?> GetIncidentByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new incident.
    /// </summary>
    /// <param name="title">Short headline.</param>
    /// <param name="description">Full description of what was reported.</param>
    /// <param name="incidentType">Name of an existing incident type.</param>
    /// <param name="status">Name of an existing status. Defaults to "Open" when null or blank.</param>
    /// <param name="location">Neighbourhood or area where the incident happened.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The saved incident.</returns>
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">
    /// The incident type or status does not exist.
    /// </exception>
    Task<IncidentDto> AddIncidentAsync(
        string title,
        string description,
        string incidentType,
        string? status,
        string location,
        CancellationToken cancellationToken = default);
}
