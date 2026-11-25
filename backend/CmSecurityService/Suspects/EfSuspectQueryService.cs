using CmSecurityService.Data;
using CmSecurityService.Incidents;
using CmSecurityService.Models;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Suspects;

/// <summary>
/// EF Core implementation of <see cref="ISuspectQueryService"/>, reading the
/// <c>suspect</c> and <c>incident</c> tables in MySQL.
/// </summary>
public class EfSuspectQueryService(AppDbContext db) : ISuspectQueryService
{
    /// <inheritdoc />
    public async Task<PagedResult<SuspectDto>> GetSuspectsAsync(
        string? search,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Suspect> query = db.Suspects.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ToLower on both sides so the search is case-insensitive in MySQL and in tests.
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                (s.FirstName != null && s.FirstName.ToLower().Contains(term)) ||
                (s.LastName != null && s.LastName.ToLower().Contains(term)) ||
                (s.KnownAliases != null && s.KnownAliases.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusName = status.Trim().ToLower();
            query = query.Where(s => s.Status.ToLower() == statusName);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenBy(s => s.SuspectId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SuspectDto
            {
                Id = s.SuspectId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                DateOfBirth = s.DateOfBirth,
                PhysicalDescription = s.PhysicalDescription,
                KnownAliases = s.KnownAliases,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SuspectDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc />
    public async Task<SuspectDto?> GetSuspectByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Suspects
            .AsNoTracking()
            .Where(s => s.SuspectId == id)
            .Select(s => new SuspectDto
            {
                Id = s.SuspectId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                DateOfBirth = s.DateOfBirth,
                PhysicalDescription = s.PhysicalDescription,
                KnownAliases = s.KnownAliases,
                Status = s.Status,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NeighborhoodRankingDto>> GetCrimeRankingsAsync(
        DateTime? from,
        DateTime? to,
        int top,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Incident> incidents = db.Incidents.AsNoTracking();

        if (from.HasValue)
        {
            var start = from.Value;
            incidents = incidents.Where(i => i.CreatedAt >= start);
        }

        if (to.HasValue)
        {
            // Same rule as View Incidents: a date with no time ("2026-10-20") means the whole day.
            var end = to.Value.TimeOfDay == TimeSpan.Zero
                ? to.Value.AddDays(1)
                : to.Value.AddTicks(1);
            incidents = incidents.Where(i => i.CreatedAt < end);
        }

        // The database counts incidents per location; only the small totals come back.
        var counts = await incidents
            .GroupBy(i => i.Location)
            .Select(g => new { Neighborhood = g.Key, IncidentCount = g.Count() })
            .ToListAsync(cancellationToken);

        return Rank(MergeSameNeighborhood(counts.Select(c => (c.Neighborhood, c.IncidentCount))), top);
    }

    /// <summary>
    /// Merges location spellings that are the same neighborhood, e.g. "Downtown", "downtown"
    /// and "Downtown " (extra space), so a neighborhood is never counted twice.
    /// The name shown is the spelling used by the most incidents, without extra spaces.
    /// </summary>
    /// <param name="counts">Incident counts per location exactly as stored.</param>
    /// <returns>One row per neighborhood with the combined count.</returns>
    public static IEnumerable<(string Neighborhood, int IncidentCount)> MergeSameNeighborhood(
        IEnumerable<(string Neighborhood, int IncidentCount)> counts)
    {
        return counts
            .Where(c => !string.IsNullOrWhiteSpace(c.Neighborhood))
            .GroupBy(c => c.Neighborhood.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => (
                Neighborhood: g.OrderByDescending(c => c.IncidentCount).First().Neighborhood.Trim(),
                IncidentCount: g.Sum(c => c.IncidentCount)));
    }

    /// <summary>
    /// Sorts neighborhoods by incident count (highest first, then by name) and numbers them.
    /// Equal counts share a rank and the next rank is skipped, e.g. 1, 2, 2, 4.
    /// </summary>
    /// <param name="counts">Neighborhood names with their incident counts.</param>
    /// <param name="top">How many rows to return.</param>
    /// <returns>At most <paramref name="top"/> ranked rows.</returns>
    public static IReadOnlyList<NeighborhoodRankingDto> Rank(
        IEnumerable<(string Neighborhood, int IncidentCount)> counts,
        int top)
    {
        var sorted = counts
            .OrderByDescending(c => c.IncidentCount)
            .ThenBy(c => c.Neighborhood, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<NeighborhoodRankingDto>();
        for (var i = 0; i < sorted.Count && i < top; i++)
        {
            var rank = i > 0 && sorted[i].IncidentCount == sorted[i - 1].IncidentCount
                ? result[i - 1].Rank
                : i + 1;

            result.Add(new NeighborhoodRankingDto
            {
                Rank = rank,
                Neighborhood = sorted[i].Neighborhood,
                IncidentCount = sorted[i].IncidentCount
            });
        }

        return result;
    }
}
