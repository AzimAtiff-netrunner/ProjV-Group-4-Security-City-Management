namespace CmSecurityService.Vehicles;

/// <summary>
/// Filter and paging options for listing vehicles (GET /api/security/v1/vehicles query string).
/// </summary>
public class VehicleQuery
{
    /// <summary>Default number of vehicles per page.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Largest page size a caller may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Optional status filter: Available, OnPatrol, Dispatched or OutOfService (case-insensitive).</summary>
    public string? Status { get; set; }

    /// <summary>Page number, starting at 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of vehicles per page (1 to <see cref="MaxPageSize"/>).</summary>
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// Checks the query for invalid values.
    /// </summary>
    /// <param name="status">Parsed status when <see cref="Status"/> is valid; otherwise null.</param>
    /// <returns>Human-readable error messages. Empty when the query is valid.</returns>
    public List<string> Validate(out VehicleStatus? status)
    {
        var errors = new List<string>();
        status = null;

        if (Page < 1)
        {
            errors.Add("page must be 1 or greater.");
        }

        if (PageSize < 1 || PageSize > MaxPageSize)
        {
            errors.Add($"pageSize must be between 1 and {MaxPageSize}.");
        }

        if (!string.IsNullOrWhiteSpace(Status))
        {
            var trimmed = Status.Trim();
            if (Enum.TryParse<VehicleStatus>(trimmed, ignoreCase: true, out var parsed)
                && Enum.IsDefined(parsed)
                && !int.TryParse(trimmed, out _))
            {
                status = parsed;
            }
            else
            {
                errors.Add($"status '{Status}' is not valid. Use one of: {string.Join(", ", Enum.GetNames<VehicleStatus>())}.");
            }
        }

        return errors;
    }
}
