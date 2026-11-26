using System.ComponentModel.DataAnnotations;

namespace CmSecurityService.Applications;

/// <summary>
/// Optional request body when approving or rejecting a police application.
/// </summary>
public record ReviewApplicationRequest(
    [StringLength(500)] string? Notes
);

/// <summary>
/// Filter and paging options for <c>GET /api/security/v1/applications</c>.
/// Defaults to <see cref="ApplicationStatus.Pending"/> per AC 1.
/// </summary>
public class ApplicationQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Optional status filter: Pending (default), Approved, Rejected, or All.</summary>
    public string? Status { get; set; } = nameof(ApplicationStatus.Pending);

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public List<string> Validate(out ApplicationStatus? status, out bool includeAll)
    {
        var errors = new List<string>();
        status = null;
        includeAll = false;

        if (Page < 1)
        {
            errors.Add("page must be 1 or greater.");
        }

        if (PageSize < 1 || PageSize > MaxPageSize)
        {
            errors.Add($"pageSize must be between 1 and {MaxPageSize}.");
        }

        if (string.IsNullOrWhiteSpace(Status))
        {
            status = ApplicationStatus.Pending;
            return errors;
        }

        var trimmed = Status.Trim();
        if (trimmed.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            includeAll = true;
            return errors;
        }

        if (Enum.TryParse<ApplicationStatus>(trimmed, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed)
            && !int.TryParse(trimmed, out _))
        {
            status = parsed;
        }
        else
        {
            errors.Add($"status '{Status}' is not valid. Use one of: All, {string.Join(", ", Enum.GetNames<ApplicationStatus>())}.");
        }

        return errors;
    }
}