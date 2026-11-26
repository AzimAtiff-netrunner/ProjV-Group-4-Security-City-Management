namespace CmSecurityService.RoadClosures;

public interface IRoadClosureService
{
    Task<IReadOnlyList<RoadClosureResponse>> GetActiveClosuresAsync(CancellationToken cancellationToken = default);
}