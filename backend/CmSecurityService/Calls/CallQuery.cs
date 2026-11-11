namespace CmSecurityService.Calls;

/// <summary>
/// Filter and paging options for listing 9-1-1 calls (GET /api/security/v1/calls query string).
/// </summary>
public class CallQuery
{
    /// <summary>Default number of calls per page.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>Largest page size a caller may request.</summary>
    public const int MaxPageSize = 100;

    /// <summary>Optional status filter: Received, Forwarded, Failed or Closed (case-insensitive).</summary>
    public string? Status { get; set; }

    /// <summary>Optional call type filter: Police, Fire or EMS (case-insensitive).</summary>
    public string? Type { get; set; }

    /// <summary>Optional start of the received date range (inclusive).</summary>
    public DateTime? From { get; set; }

    /// <summary>Optional end of the received date range (inclusive).</summary>
    public DateTime? To { get; set; }

    /// <summary>Page number, starting at 1.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Number of calls per page (1 to <see cref="MaxPageSize"/>).</summary>
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// Checks the query for invalid values.
    /// </summary>
    /// <param name="status">Parsed status when <see cref="Status"/> is valid; otherwise null.</param>
    /// <param name="type">Parsed call type when <see cref="Type"/> is valid; otherwise null.</param>
    /// <returns>Human-readable error messages. Empty when the query is valid.</returns>
    public List<string> Validate(out CallStatus? status, out CallType? type)
    {
        var errors = new List<string>();
        status = null;
        type = null;

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
            if (TryParseName<CallStatus>(Status, out var parsed))
            {
                status = parsed;
            }
            else
            {
                errors.Add($"status '{Status}' is not valid. Use one of: {string.Join(", ", Enum.GetNames<CallStatus>())}.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Type))
        {
            if (TryParseName<CallType>(Type, out var parsed))
            {
                type = parsed;
            }
            else
            {
                errors.Add($"type '{Type}' is not valid. Use one of: {string.Join(", ", Enum.GetNames<CallType>())}.");
            }
        }

        if (From.HasValue && To.HasValue && From.Value > To.Value)
        {
            errors.Add("from must be earlier than or equal to to.");
        }

        return errors;
    }

    /// <summary>
    /// Parses an enum by name only (case-insensitive); numbers such as "1" are rejected.
    /// </summary>
    private static bool TryParseName<TEnum>(string value, out TEnum result) where TEnum : struct, Enum
    {
        var trimmed = value.Trim();
        return Enum.TryParse(trimmed, ignoreCase: true, out result)
               && Enum.IsDefined(result)
               && !int.TryParse(trimmed, out _);
    }
}
