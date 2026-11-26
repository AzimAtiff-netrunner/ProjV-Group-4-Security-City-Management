using CmSecurityService.Data;
using CmSecurityService.Incidents;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Applications;

/// <summary>
/// <see cref="IPoliceApplicationService"/> backed by MySQL via EF Core.
/// </summary>
public class EfPoliceApplicationService : IPoliceApplicationService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public EfPoliceApplicationService(AppDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PagedResult<PoliceApplicationDto>> GetApplicationsAsync(
        ApplicationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PoliceApplications.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PoliceApplicationDto>
        {
            Items = rows.Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PoliceApplicationDto?> GetApplicationByIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.PoliceApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId, cancellationToken);

        return row is null ? null : ToDto(row);
    }

    public Task<PoliceApplicationDto?> ApproveApplicationAsync(
        Guid applicationId,
        string? reviewerUserId,
        string? notes,
        CancellationToken cancellationToken = default) =>
        ReviewAsync(applicationId, ApplicationStatus.Approved, reviewerUserId, notes, cancellationToken);

    public Task<PoliceApplicationDto?> RejectApplicationAsync(
        Guid applicationId,
        string? reviewerUserId,
        string? notes,
        CancellationToken cancellationToken = default) =>
        ReviewAsync(applicationId, ApplicationStatus.Rejected, reviewerUserId, notes, cancellationToken);

    private async Task<PoliceApplicationDto?> ReviewAsync(
        Guid applicationId,
        ApplicationStatus targetStatus,
        string? reviewerUserId,
        string? notes,
        CancellationToken cancellationToken)
    {
        var application = await _db.PoliceApplications
            .FirstOrDefaultAsync(a => a.ApplicationId == applicationId, cancellationToken);

        if (application is null)
        {
            return null;
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            throw new ApplicationAlreadyFinalizedException(applicationId, application.Status);
        }

        string? officerId = null;
        if (!string.IsNullOrWhiteSpace(reviewerUserId))
        {
            officerId = await _db.Officers
                .AsNoTracking()
                .Where(o => o.UserId == reviewerUserId && o.IsActive)
                .Select(o => o.OfficerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        application.Status = targetStatus;
        application.ReviewedAt = TruncateToMicroseconds(_clock.GetUtcNow().UtcDateTime);
        application.ReviewedByOfficerId = officerId;
        application.ReviewerNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(application);
    }

    private static PoliceApplicationDto ToDto(PoliceApplication row) => new()
    {
        ApplicationId = row.ApplicationId,
        UserId = row.UserId,
        FirstName = row.FirstName,
        LastName = row.LastName,
        Email = row.Email,
        Phone = row.Phone,
        DateOfBirth = row.DateOfBirth,
        Address = row.Address,
        Qualifications = row.Qualifications,
        ExperienceYears = row.ExperienceYears,
        Status = row.Status.ToString(),
        ReviewerNotes = row.ReviewerNotes,
        ReviewedByOfficerId = row.ReviewedByOfficerId,
        SubmittedAt = AsUtc(row.SubmittedAt),
        ReviewedAt = row.ReviewedAt.HasValue ? AsUtc(row.ReviewedAt.Value) : null
    };

    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime TruncateToMicroseconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Utc);
}