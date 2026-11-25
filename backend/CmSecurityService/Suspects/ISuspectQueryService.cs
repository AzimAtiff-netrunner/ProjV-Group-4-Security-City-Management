using CmSecurityService.Incidents;

namespace CmSecurityService.Suspects;

/// <summary>
/// Reads suspect records and neighborhood crime rankings (story #225).
/// </summary>
public interface ISuspectQueryService
{
    /// <summary>
    /// Lists suspects, newest first.
    /// </summary>
    /// <param name="search">Optional text to find in names or aliases.</param>
    /// <param name="status">Optional status, e.g. "Under investigation".</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Suspects per page.</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>One page of suspects.</returns>
    Task<PagedResult<SuspectDto>> GetSuspectsAsync(
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets one suspect.
    /// </summary>
    /// <param name="id">Suspect identifier.</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>The suspect, or null if no suspect has this id.</returns>
    Task<SuspectDto?> GetSuspectByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ranks neighborhoods by number of reported incidents, highest first.
    /// </summary>
    /// <param name="from">Optional start date (inclusive).</param>
    /// <param name="to">Optional end date (inclusive).</param>
    /// <param name="top">How many neighborhoods to return.</param>
    /// <param name="cancellationToken">Cancelled if the caller disconnects.</param>
    /// <returns>The ranked neighborhoods.</returns>
    Task<IReadOnlyList<NeighborhoodRankingDto>> GetCrimeRankingsAsync(
        DateTime? from,
        DateTime? to,
        int top,
        CancellationToken cancellationToken = default);
}
