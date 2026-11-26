namespace CmSecurityService.Applications;

/// <summary>
/// Review status of a police department application. Stored by name in <c>police_application.status</c>.
/// </summary>
public enum ApplicationStatus
{
    Pending,
    Approved,
    Rejected
}