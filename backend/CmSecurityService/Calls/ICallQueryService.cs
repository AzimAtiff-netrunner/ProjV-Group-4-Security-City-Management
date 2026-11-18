using CmSecurityService.Incidents;

namespace CmSecurityService.Calls;

/// <summary>
/// Read-only access to 9-1-1 calls.
/// </summary>
/// <remarks>
/// The controller depends on this interface, not on a concrete data source.
/// Today it is backed by <see cref="InMemoryCallQueryService"/> (seed data).
/// When the emergency_call table exists, add an EF Core implementation and swap the
/// registration in <see cref="CallServiceCollectionExtensions"/>; the controller does not change.
/// </remarks>
public interface ICallQueryService
{
    /// <summary>
    /// Returns one page of calls matching the filters, newest first.
    /// </summary>
    /// <param name="status">Optional status filter.</param>
    /// <param name="type">Optional call type filter.</param>
    /// <param name="from">Optional start of received date range (inclusive).</param>
    /// <param name="to">Optional end of received date range (inclusive).</param>
    /// <param name="page">Page number, starting at 1.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A page of calls and the total match count.</returns>
    Task<PagedResult<CallDto>> GetCallsAsync(
        CallStatus? status,
        CallType? type,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single call by id.
    /// </summary>
    /// <param name="id">Call identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The call, or null if no call has that id.</returns>
    Task<CallDto?> GetCallByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
