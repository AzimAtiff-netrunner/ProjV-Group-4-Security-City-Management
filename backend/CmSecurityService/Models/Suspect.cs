namespace CmSecurityService.Models;

/// <summary>
/// A row of the <c>suspect</c> table in security_db (story #225).
/// </summary>
/// <remarks>
/// Column names and sizes are mapped in <see cref="CmSecurityService.Data.AppDbContext"/>.
/// </remarks>
public class Suspect
{
    /// <summary>suspect_id: CHAR(36) primary key.</summary>
    public Guid SuspectId { get; set; }

    /// <summary>first_name: may be unknown (NULL).</summary>
    public string? FirstName { get; set; }

    /// <summary>last_name: may be unknown (NULL).</summary>
    public string? LastName { get; set; }

    /// <summary>date_of_birth: DATE, may be unknown.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>physical_description: free text.</summary>
    public string? PhysicalDescription { get; set; }

    /// <summary>known_aliases: comma-separated nicknames.</summary>
    public string? KnownAliases { get; set; }

    /// <summary>status: defaults to "Under investigation" in the database.</summary>
    public string Status { get; set; } = "Under investigation";

    /// <summary>created_at: when the record was added.</summary>
    public DateTime CreatedAt { get; set; }
}
