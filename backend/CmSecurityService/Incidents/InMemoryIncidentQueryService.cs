namespace CmSecurityService.Incidents;

/// <summary>
/// Temporary <see cref="IIncidentQueryService"/> backed by in-memory seed data.
/// </summary>
/// <remarks>
/// Lets View Incidents (#216) be built, tested and demoed before the database (#215)
/// and Register Incidents (#217) are merged. Replace with an EF Core implementation
/// once those are on main.
/// </remarks>
public class InMemoryIncidentQueryService : IIncidentQueryService
{
    /// <summary>
    /// Storage model for the seed data. Mirrors what a database row would hold,
    /// including internal fields that must never appear in <see cref="IncidentDto"/>.
    /// </summary>
    public class IncidentRecord
    {
        /// <summary>Unique incident identifier.</summary>
        public int Id { get; set; }
        /// <summary>Short headline.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Full description.</summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>Incident category.</summary>
        public string IncidentType { get; set; } = string.Empty;
        /// <summary>Lifecycle status.</summary>
        public IncidentStatus Status { get; set; }
        /// <summary>Neighbourhood or area.</summary>
        public string Location { get; set; } = string.Empty;
        /// <summary>When reported (UTC).</summary>
        public DateTime ReportedAt { get; set; }
        /// <summary>Internal officer notes. Not exposed through the API.</summary>
        public string InternalNotes { get; set; } = string.Empty;
        /// <summary>Soft-delete flag. Deleted incidents are hidden from callers.</summary>
        public bool IsDeleted { get; set; }
    }

    private readonly IReadOnlyList<IncidentRecord> _records;

    /// <summary>
    /// Creates the service with the built-in demo seed data.
    /// </summary>
    public InMemoryIncidentQueryService() : this(SeedData()) { }

    /// <summary>
    /// Creates the service with caller-supplied records (used by unit tests).
    /// </summary>
    /// <param name="records">Records to serve.</param>
    public InMemoryIncidentQueryService(IEnumerable<IncidentRecord> records)
    {
        _records = records.ToList();
    }

    /// <inheritdoc />
    public Task<PagedResult<IncidentDto>> GetIncidentsAsync(
        IncidentStatus? status,
        string? type,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<IncidentRecord> query = _records.Where(r => !r.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(r => string.Equals(r.IncidentType, type.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.ReportedAt >= from.Value);
        }

        if (to.HasValue)
        {
            // A date with no time (e.g. 2026-10-20) means "the whole day".
            var end = to.Value.TimeOfDay == TimeSpan.Zero ? to.Value.AddDays(1) : to.Value.AddTicks(1);
            query = query.Where(r => r.ReportedAt < end);
        }

        var matches = query.OrderByDescending(r => r.ReportedAt).ThenByDescending(r => r.Id).ToList();

        var result = new PagedResult<IncidentDto>
        {
            Items = matches.Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = matches.Count
        };

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<IncidentDto?> GetIncidentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var record = _records.FirstOrDefault(r => r.Id == id && !r.IsDeleted);
        return Task.FromResult(record is null ? null : ToDto(record));
    }

    /// <summary>
    /// Copies only the public fields of a record into a DTO.
    /// </summary>
    /// <param name="r">Storage record.</param>
    /// <returns>The public view of the incident.</returns>
    public static IncidentDto ToDto(IncidentRecord r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Description = r.Description,
        IncidentType = r.IncidentType,
        Status = r.Status.ToString(),
        Location = r.Location,
        ReportedAt = r.ReportedAt
    };

    /// <summary>
    /// Demo incidents used until the real database is connected.
    /// </summary>
    /// <returns>Fictional seed records.</returns>
    public static List<IncidentRecord> SeedData() => new()
    {
        new() { Id = 1, Title = "Bike stolen from rack", Description = "Locked bicycle taken from the rack outside the public library.", IncidentType = "Theft", Status = IncidentStatus.Open, Location = "Downtown", ReportedAt = new DateTime(2026, 10, 14, 9, 30, 0, DateTimeKind.Utc), InternalNotes = "Check library CCTV." },
        new() { Id = 2, Title = "Graffiti on bus shelter", Description = "Spray paint on the bus shelter at King and Main.", IncidentType = "Vandalism", Status = IncidentStatus.Closed, Location = "Uptown", ReportedAt = new DateTime(2026, 10, 15, 14, 5, 0, DateTimeKind.Utc), InternalNotes = "Cleaned by maintenance." },
        new() { Id = 3, Title = "Break-in at convenience store", Description = "Rear door forced open overnight; cash register emptied.", IncidentType = "Burglary", Status = IncidentStatus.UnderInvestigation, Location = "Eastside", ReportedAt = new DateTime(2026, 10, 16, 6, 45, 0, DateTimeKind.Utc), InternalNotes = "Possible link to incident 7." },
        new() { Id = 4, Title = "Two-car collision", Description = "Minor collision at the Elm Street intersection, no injuries.", IncidentType = "Traffic", Status = IncidentStatus.Closed, Location = "Westside", ReportedAt = new DateTime(2026, 10, 17, 17, 20, 0, DateTimeKind.Utc), InternalNotes = "Both drivers exchanged insurance." },
        new() { Id = 5, Title = "Noise complaint", Description = "Loud music reported from a residence after midnight.", IncidentType = "Disturbance", Status = IncidentStatus.Closed, Location = "Northgate", ReportedAt = new DateTime(2026, 10, 18, 4, 10, 0, DateTimeKind.Utc), InternalNotes = "Warning issued." },
        new() { Id = 6, Title = "Wallet taken on transit", Description = "Passenger reports wallet stolen on route 7 bus.", IncidentType = "Theft", Status = IncidentStatus.UnderInvestigation, Location = "Downtown", ReportedAt = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc), InternalNotes = "Requested footage from Public Transport." },
        new() { Id = 7, Title = "Attempted break-in", Description = "Pry marks found on the side door of a pharmacy.", IncidentType = "Burglary", Status = IncidentStatus.Open, Location = "Eastside", ReportedAt = new DateTime(2026, 10, 20, 7, 55, 0, DateTimeKind.Utc), InternalNotes = "Same tool marks as incident 3." },
        new() { Id = 8, Title = "Abandoned vehicle", Description = "Car parked on the street for over a week without plates.", IncidentType = "Traffic", Status = IncidentStatus.Open, Location = "Westside", ReportedAt = new DateTime(2026, 10, 21, 10, 15, 0, DateTimeKind.Utc), InternalNotes = "Tow requested." },
        new() { Id = 9, Title = "Duplicate report", Description = "Entered twice by mistake.", IncidentType = "Theft", Status = IncidentStatus.Open, Location = "Downtown", ReportedAt = new DateTime(2026, 10, 14, 9, 31, 0, DateTimeKind.Utc), InternalNotes = "Duplicate of 1.", IsDeleted = true }
    };
}
