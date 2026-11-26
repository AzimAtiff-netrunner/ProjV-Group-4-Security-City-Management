namespace CmSecurityService.RoadClosures;

public class InMemoryRoadClosureService : IRoadClosureService
{
    private readonly List<RoadClosure> _closures;

    public InMemoryRoadClosureService()
    {
        var now = DateTimeOffset.UtcNow;

        _closures = new List<RoadClosure>
        {
            new RoadClosure
            {
                ClosureId = Guid.NewGuid(),
                RoadName = "King Street",
                Description = "Road closed due to construction.",
                Location = "Between Queen Street and Ontario Street",
                StartDate = now.AddHours(-2),
                EndDate = now.AddHours(4),
                Status = "Active",
                LastUpdated = now
            },

            new RoadClosure
            {
                ClosureId = Guid.NewGuid(),
                RoadName = "Weber Street",
                Description = "Lane closure due to emergency repairs.",
                Location = "Near Frederick Street",
                StartDate = now.AddHours(-1),
                EndDate = null,
                Status = "Active",
                LastUpdated = now
            },

            new RoadClosure
            {
                ClosureId = Guid.NewGuid(),
                RoadName = "Victoria Street",
                Description = "Previous road closure.",
                Location = "Near Lancaster Street",
                StartDate = now.AddDays(-2),
                EndDate = now.AddHours(-5),
                Status = "Inactive",
                LastUpdated = now
            }
        };
    }

    public Task<IReadOnlyList<RoadClosureResponse>> GetActiveClosuresAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;

        var activeClosures = _closures
            .Where(closure =>
                closure.Status == "Active" &&
                (closure.EndDate == null || closure.EndDate > now))
            .Select(closure => new RoadClosureResponse
            {
                ClosureId = closure.ClosureId,
                RoadName = closure.RoadName,
                Description = closure.Description,
                Location = closure.Location,
                StartDate = closure.StartDate,
                EndDate = closure.EndDate,
                Status = closure.Status,
                LastUpdated = closure.LastUpdated
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<RoadClosureResponse>>(activeClosures);
    }
}