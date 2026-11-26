using CmSecurityService.Incidents;

namespace CmSecurityService.Applications;

/// <summary>
/// Retrieves and reviews police applications for Security Administrators (User Story #222).
/// </summary>
public interface IPoliceApplicationService
{
    Task<PagedResult<PoliceApplicationDto>> GetApplicationsAsync(
        ApplicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PoliceApplicationDto?> GetApplicationByIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<PoliceApplicationDto?> ApproveApplicationAsync(
        Guid applicationId,
        string? reviewerUserId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<PoliceApplicationDto?> RejectApplicationAsync(
        Guid applicationId,
        string? reviewerUserId,
        string? notes,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Thrown when an administrator attempts to approve or reject an application
/// that has already been finalized (AC 7).
/// </summary>
public class ApplicationAlreadyFinalizedException : Exception
{
    public Guid ApplicationId { get; }
    public ApplicationStatus CurrentStatus { get; }

    public ApplicationAlreadyFinalizedException(Guid applicationId, ApplicationStatus currentStatus)
        : base($"Application '{applicationId}' has already been finalized with status '{currentStatus}'.")
    {
        ApplicationId = applicationId;
        CurrentStatus = currentStatus;
    }
}