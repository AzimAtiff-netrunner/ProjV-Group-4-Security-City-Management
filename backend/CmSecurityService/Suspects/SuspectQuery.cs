namespace CmSecurityService.Suspects;

/// <summary>
/// Filter and paging options for GET /api/security/v1/suspects.
/// </summary>
public class SuspectQuery
{
    /// <summary>Default number of suspects per page.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Largest page size a caller may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Longest search text accepted.</summary>
    public const int MaxSearchLength = 100;

    /// <summary>Optional text to find in the first name, last name or known aliases (case-insensitive).</summary>
    public string? Search { get; set; }

    /// <summary>Optional status filter, e.g. "Under investigation" (case-insensitive exact match).</summary>
    public string? Status { get; set; }

    /// <summary>Page number, starting at 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of suspects per page (1 to <see cref="MaxPageSize"/>).</summary>
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// Checks the query for invalid values.
    /// </summary>
    /// <returns>Human-readable error messages. Empty when the query is valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Page < 1)
        {
            errors.Add("page must be 1 or greater.");
        }

        if (PageSize < 1 || PageSize > MaxPageSize)
        {
            errors.Add($"pageSize must be between 1 and {MaxPageSize}.");
        }

        if (Search is not null && Search.Trim().Length > MaxSearchLength)
        {
            errors.Add($"search must be {MaxSearchLength} characters or fewer.");
        }

        return errors;
    }
}
