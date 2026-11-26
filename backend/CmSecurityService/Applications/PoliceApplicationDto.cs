namespace CmSecurityService.Applications;

/// <summary>
/// Public representation of a police application returned to Security Administrators.
/// </summary>
public class PoliceApplicationDto
{
    public Guid ApplicationId { get; set; }

    public string? UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public DateOnly DateOfBirth { get; set; }

    public string Address { get; set; } = string.Empty;

    public string? Qualifications { get; set; }

    public uint ExperienceYears { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ReviewerNotes { get; set; }

    public string? ReviewedByOfficerId { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }
}