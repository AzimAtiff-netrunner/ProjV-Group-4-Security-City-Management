using System.ComponentModel.DataAnnotations;
using CmSecurityService.Data;
using CmSecurityService.Models;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Incidents;

/// <summary>
/// EF Core implementation of <see cref="IIncidentQueryService"/> backed by the MySQL database.
/// </summary>
public class EfIncidentQueryService(AppDbContext db) : IIncidentQueryService
{
    /// <inheritdoc />
    public async Task<PagedResult<IncidentDto>> GetIncidentsAsync(
        string? status,
        string? type,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Incident> query = db.Incidents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusName = status.Trim();
            var statusExists = await db.IncidentStatuses
                .AnyAsync(s => s.Name == statusName, cancellationToken);
            if (!statusExists)
            {
                var valid = await db.IncidentStatuses
                    .Select(s => s.Name).ToListAsync(cancellationToken);
                throw new ValidationException(
                    $"status '{statusName}' is not valid. Use one of: {string.Join(", ", valid)}.");
            }

            query = query.Where(i => i.IncidentStatus!.Name == statusName);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var typeName = type.Trim();
            query = query.Where(i => i.IncidentType!.Name == typeName);
        }

        if (from.HasValue)
        {
            var start = from.Value;
            query = query.Where(i => i.CreatedAt >= start);
        }

        if (to.HasValue)
        {
            // A date with no time ("2026-10-20") means the whole day, as in the in-memory version.
            var end = to.Value.TimeOfDay == TimeSpan.Zero
                ? to.Value.AddDays(1)
                : to.Value.AddTicks(1);
            query = query.Where(i => i.CreatedAt < end);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .ThenByDescending(i => i.IncidentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new IncidentDto
            {
                Id = i.IncidentId,
                Title = i.Title,
                Description = i.Description,
                IncidentType = i.IncidentType!.Name,
                Status = i.IncidentStatus!.Name,
                Location = i.Location,
                ReportedAt = i.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<IncidentDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<IncidentDto?> GetIncidentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await db.Incidents
            .AsNoTracking()
            .Where(i => i.IncidentId == id)
            .Select(i => new IncidentDto
            {
                Id = i.IncidentId,
                Title = i.Title,
                Description = i.Description,
                IncidentType = i.IncidentType!.Name,
                Status = i.IncidentStatus!.Name,
                Location = i.Location,
                ReportedAt = i.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IncidentDto> AddIncidentAsync(
        string title,
        string description,
        string incidentType,
        string? status,
        string location,
        CancellationToken cancellationToken = default)
    {
        var typeName = incidentType.Trim();
        var type = await db.IncidentTypes
            .FirstOrDefaultAsync(t => t.Name == typeName, cancellationToken)
            ?? throw new ValidationException($"Unknown incident type '{typeName}'.");

        var statusName = string.IsNullOrWhiteSpace(status) ? "Open" : status.Trim();
        var statusRow = await db.IncidentStatuses
            .FirstOrDefaultAsync(s => s.Name == statusName, cancellationToken)
            ?? throw new ValidationException($"Unknown status '{statusName}'.");

        var incident = new Incident
        {
            IncidentId = Guid.NewGuid(),
            IncidentTypeId = type.IncidentTypeId,
            IncidentStatusId = statusRow.IncidentStatusId,
            Title = title.Trim(),
            Description = description.Trim(),
            Location = location.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        db.Incidents.Add(incident);
        await db.SaveChangesAsync(cancellationToken);   // one SaveChanges is atomic

        return new IncidentDto
        {
            Id = incident.IncidentId,
            Title = incident.Title,
            Description = incident.Description,
            IncidentType = type.Name,
            Status = statusRow.Name,
            Location = incident.Location,
            ReportedAt = incident.CreatedAt
        };
    }
}