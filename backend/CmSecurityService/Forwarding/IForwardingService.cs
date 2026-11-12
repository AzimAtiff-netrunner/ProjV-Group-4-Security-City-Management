namespace CmSecurityService.Forwarding;

public interface IForwardingService
{
    bool RequiresEmergencyForwarding(string incidentType, string description, out string recommendedService);
    Task<ForwardingRecord> ForwardCallAsync(ForwardCallRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<ForwardingRecord> GetForwardingHistory(int? callId = null);
}
