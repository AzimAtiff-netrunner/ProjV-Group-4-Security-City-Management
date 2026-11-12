namespace CmSecurityService.Forwarding;

public interface IEmergencyServiceClient
{
    Task<EmergencyServiceResponse?> ForwardIncidentAsync(
        EmergencyIncidentPayload payload,
        CancellationToken cancellationToken = default
    );
}
