namespace CmSecurityService.Suspects;

/// <summary>
/// A suspect record as returned by GET /api/security/v1/suspects (story #225).
/// </summary>
public class SuspectDto
{
    /// <summary>Suspect identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>First name, or null if unknown.</summary>
    public string? FirstName { get; set; }

    /// <summary>Last name, or null if unknown.</summary>
    public string? LastName { get; set; }

    /// <summary>Date of birth, or null if unknown.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Physical description (height, build, clothing...).</summary>
    public string? PhysicalDescription { get; set; }

    /// <summary>Known aliases, comma-separated.</summary>
    public string? KnownAliases { get; set; }

    /// <summary>Investigation status, e.g. "Under investigation".</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>When the record was created (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}
