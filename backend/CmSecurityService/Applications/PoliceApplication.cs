using CmSecurityService.Models;

namespace CmSecurityService.Applications;

/// <summary>
/// Storage model for the <c>police_application</c> table.
/// </summary>
public class PoliceApplication
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

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public string? ReviewerNotes { get; set; }

    public string? ReviewedByOfficerId { get; set; }

    public DateTime SubmittedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public Officer? ReviewedByOfficer { get; set; }
}