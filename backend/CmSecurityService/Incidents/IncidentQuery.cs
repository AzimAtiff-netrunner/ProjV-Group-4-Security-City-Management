namespace CmSecurityService.Incidents;

/// <summary>
/// Filter and paging options for listing incidents (GET /incidents query string).
/// </summary>
public class IncidentQuery
{
    /// <summary>Default number of incidents per page.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Largest page size a caller may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Optional status filter: Open, UnderInvestigation or Closed (case-insensitive).</summary>
    public string? Status { get; set; }

    /// <summary>Optional incident type filter, e.g. "Theft" (case-insensitive exact match).</summary>
    public string? Type { get; set; }

    /// <summary>Optional start of the reported date range (inclusive).</summary>
    public DateTime? From { get; set; }

    /// <summary>Optional end of the reported date range (inclusive).</summary>
    public DateTime? To { get; set; }

    /// <summary>Page number, starting at 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of incidents per page (1 to <see cref="MaxPageSize"/>).</summary>
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// Checks the query for invalid values.
    /// </summary>
    /// <param name="status">Parsed status when <see cref="Status"/> is valid; otherwise null.</param>
    /// <returns>A list of human-readable error messages. Empty when the query is valid.</returns>
    public List<string> Validate(out IncidentStatus? status)
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
            // Enum.TryParse also accepts numbers like "1"; reject those so only names are valid.
            if (Enum.TryParse<IncidentStatus>(Status, ignoreCase: true, out var parsed)
                && Enum.IsDefined(parsed)
                && !int.TryParse(Status, out _))
            {
                status = parsed;
            }
            else
            {
                errors.Add($"status '{Status}' is not valid. Use one of: {string.Join(", ", Enum.GetNames<IncidentStatus>())}.");
            }
        }

        if (From.HasValue && To.HasValue && From.Value > To.Value)
        {
            errors.Add("from must be earlier than or equal to to.");
        }

        return errors;
    }
}
