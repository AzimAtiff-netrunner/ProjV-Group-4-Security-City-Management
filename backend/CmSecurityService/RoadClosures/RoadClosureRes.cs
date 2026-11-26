namespace CmSecurityService.RoadClosures;

public class RoadClosureResponse
{
    public Guid ClosureId { get; set; }

    public string RoadName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset LastUpdated { get; set; }
}