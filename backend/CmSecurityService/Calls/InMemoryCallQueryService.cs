using CmSecurityService.Incidents;

namespace CmSecurityService.Calls;

/// <summary>
/// Temporary <see cref="ICallQueryService"/> backed by in-memory seed data.
/// </summary>
/// <remarks>
/// Lets View 9-1-1 Calls be built and demoed before the emergency_call table exists.
/// Replace with an EF Core implementation once the table is in the database.
/// </remarks>
public class InMemoryCallQueryService : ICallQueryService
{
    /// <summary>
    /// Storage model for the seed data. Mirrors the proposed emergency_call row,
    /// including internal fields that must never appear in <see cref="CallDto"/>.
    /// </summary>
    public class CallRecord
    {
        /// <summary>Unique call identifier.</summary>
        public Guid Id { get; set; }
        /// <summary>Caller's name, if given.</summary>
        public string? CallerName { get; set; }
        /// <summary>Caller's phone number.</summary>
        public string? CallerPhone { get; set; }
        /// <summary>Where the emergency is.</summary>
        public string Location { get; set; } = string.Empty;
        /// <summary>Police, Fire or EMS.</summary>
        public CallType CallType { get; set; }
        /// <summary>What the caller reported.</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>Lifecycle status.</summary>
        public CallStatus Status { get; set; }
        /// <summary>When received (UTC).</summary>
        public DateTime ReceivedAt { get; set; }
        /// <summary>When forwarded (UTC), if it was.</summary>
        public DateTime? ForwardedAt { get; set; }
        /// <summary>Internal dispatcher notes. Not exposed through the API.</summary>
        public string OperatorNotes { get; set; } = string.Empty;
    }

    private readonly IReadOnlyList<CallRecord> _records;

    /// <summary>
    /// Creates the service with the built-in demo seed data.
    /// </summary>
    public InMemoryCallQueryService() : this(SeedData()) { }

    /// <summary>
    /// Creates the service with caller-supplied records (for tests).
    /// </summary>
    /// <param name="records">Records to serve.</param>
    public InMemoryCallQueryService(IEnumerable<CallRecord> records)
    {
        _records = records.ToList();
    }

    /// <inheritdoc />
    public Task<PagedResult<CallDto>> GetCallsAsync(
        CallStatus? status,
        CallType? type,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<CallRecord> query = _records;

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(r => r.CallType == type.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.ReceivedAt >= from.Value);
        }

        if (to.HasValue)
        {
            // A date with no time (e.g. 2026-11-10) means "the whole day".
            var end = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.AddDays(1) : to.Value.AddTicks(1);
            query = query.Where(r => r.ReceivedAt < end);
        }

        var matches = query.OrderByDescending(r => r.ReceivedAt).ToList();

        var result = new PagedResult<CallDto>
        {
            Items = matches.Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = matches.Count
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<CallDto?> GetCallByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var record = _records.FirstOrDefault(r => r.Id == id);
        return Task.FromResult(record is null ? null : ToDto(record));
    }

    /// <summary>
    /// Copies only the public fields of a record into a DTO.
    /// </summary>
    /// <param name="r">Storage record.</param>
    /// <returns>The public view of the call.</returns>
    public static CallDto ToDto(CallRecord r) => new()
    {
        Id = r.Id,
        CallerName = r.CallerName,
        CallerPhone = r.CallerPhone,
        Location = r.Location,
        CallType = r.CallType.ToString(),
        Description = r.Description,
        Status = r.Status.ToString(),
        ReceivedAt = r.ReceivedAt,
        ForwardedAt = r.ForwardedAt
    };

    /// <summary>
    /// Demo 9-1-1 calls used until the real table exists. All names and numbers are fictional.
    /// </summary>
    /// <returns>Seed records.</returns>
    public static List<CallRecord> SeedData() => new()
    {
        new() { Id = Guid.Parse("11111111-1111-1111-1111-000000000001"), CallerName = "J. Smith", CallerPhone = "555-0101", Location = "King St & Main St", CallType = CallType.Police, Description = "Shoplifter running from convenience store.", Status = CallStatus.Closed, ReceivedAt = new DateTime(2026, 11, 3, 14, 12, 0, DateTimeKind.Utc), OperatorNotes = "Officer 214 attended." },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-000000000002"), CallerName = "A. Patel", CallerPhone = "555-0102", Location = "45 Elm Ave", CallType = CallType.Fire, Description = "Smoke coming from kitchen window.", Status = CallStatus.Forwarded, ReceivedAt = new DateTime(2026, 11, 4, 19, 40, 0, DateTimeKind.Utc), ForwardedAt = new DateTime(2026, 11, 4, 19, 41, 0, DateTimeKind.Utc), OperatorNotes = "Fire dept confirmed." },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-000000000003"), CallerName = null, CallerPhone = "555-0103", Location = "Victoria Park", CallType = CallType.EMS, Description = "Person collapsed near the playground.", Status = CallStatus.Forwarded, ReceivedAt = new DateTime(2026, 11, 6, 8, 5, 0, DateTimeKind.Utc), ForwardedAt = new DateTime(2026, 11, 6, 8, 6, 0, DateTimeKind.Utc), OperatorNotes = "Caller stayed on line." },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-000000000004"), CallerName = "M. Chen", CallerPhone = "555-0104", Location = "Uptown Plaza parking lot", CallType = CallType.Police, Description = "Car window smashed, items taken.", Status = CallStatus.Received, ReceivedAt = new DateTime(2026, 11, 8, 22, 30, 0, DateTimeKind.Utc), OperatorNotes = "Awaiting available unit." },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-000000000005"), CallerName = "R. Garcia", CallerPhone = "555-0105", Location = "Highway 8 eastbound", CallType = CallType.EMS, Description = "Two-car collision, one driver injured.", Status = CallStatus.Failed, ReceivedAt = new DateTime(2026, 11, 9, 17, 15, 0, DateTimeKind.Utc), OperatorNotes = "Forwarding timed out; retry pending." },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-000000000006"), CallerName = "L. Brown", CallerPhone = "555-0106", Location = "12 Oak St", CallType = CallType.Police, Description = "Loud argument, possible domestic dispute.", Status = CallStatus.InProgress, ReceivedAt = new DateTime(2026, 11, 10, 1, 50, 0, DateTimeKind.Utc), OperatorNotes = "Priority unit requested." },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-000000000007"), CallerName = "S. Nguyen", CallerPhone = "555-0107", Location = "Riverside apartments, unit 3B", CallType = CallType.Fire, Description = "Fire alarm sounding, smell of gas.", Status = CallStatus.Forwarded, ReceivedAt = new DateTime(2026, 11, 10, 12, 20, 0, DateTimeKind.Utc), ForwardedAt = new DateTime(2026, 11, 10, 12, 21, 0, DateTimeKind.Utc), OperatorNotes = "Gas company notified." },
        new() { Id = Guid.Parse("11111111-1111-1111-1111-000000000008"), CallerName = "D. Wilson", CallerPhone = "555-0108", Location = "Downtown transit terminal", CallType = CallType.Police, Description = "Suspicious unattended bag.", Status = CallStatus.Received, ReceivedAt = new DateTime(2026, 11, 11, 9, 45, 0, DateTimeKind.Utc), OperatorNotes = "Transit security on scene." }
    };
}
