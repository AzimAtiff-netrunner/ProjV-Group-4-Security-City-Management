using System.Collections.Concurrent;

namespace CmSecurityService.Forwarding;

public class InMemoryForwardingService : IForwardingService
{
    private readonly IEmergencyServiceClient _emergencyClient;
    private readonly ILogger<InMemoryForwardingService> _logger;
    private readonly ConcurrentDictionary<int, ForwardingRecord> _recordsByCallId = new();

    private static readonly string[] FireKeywords = ["fire", "smoke", "explosion", "gas leak", "arson", "blaze"];
    private static readonly string[] EmsKeywords = ["medical", "ems", "ambulance", "injury", "unconscious", "overdose", "cardiac", "bleeding"];

    public InMemoryForwardingService(
        IEmergencyServiceClient emergencyClient,
        ILogger<InMemoryForwardingService> logger)
    {
        _emergencyClient = emergencyClient;
        _logger = logger;
    }

    public bool RequiresEmergencyForwarding(string incidentType, string description, out string recommendedService)
    {
        var combined = $"{incidentType} {description}".ToLowerInvariant();

        if (FireKeywords.Any(k => combined.Contains(k)))
        {
            recommendedService = "Fire";
            return true;
        }

        if (EmsKeywords.Any(k => combined.Contains(k)))
        {
            recommendedService = "EMS";
            return true;
        }

        recommendedService = "Police";
        return false;
    }

    public async Task<ForwardingRecord> ForwardCallAsync(
        ForwardCallRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_recordsByCallId.TryGetValue(request.CallId, out var existing) && existing.Status == "Forwarded")
        {
            _logger.LogInformation(
                "Call {CallId} was already forwarded (Ref: {RefId}). Returning existing record.",
                request.CallId,
                existing.ExternalReferenceId);
            return existing;
        }

        var attemptNumber = (existing?.AttemptCount ?? 0) + 1;
        var idempotencyKey = $"cm-security-call-{request.CallId}-{request.TargetService.ToLowerInvariant()}";

        var payload = new EmergencyIncidentPayload(
            ExternalSource: "CmSecurityService",
            IncidentType: request.TargetService,
            Description: string.IsNullOrWhiteSpace(request.DispatcherNotes)
                ? $"Forwarded 9-1-1 call #{request.CallId} requiring {request.TargetService} response."
                : request.DispatcherNotes,
            Location: "City Sector - Dispatched via Security Console",
            Priority: "High",
            ReportedAtUtc: DateTime.UtcNow,
            IdempotencyKey: idempotencyKey
        );

        var response = await _emergencyClient.ForwardIncidentAsync(payload, cancellationToken);

        ForwardingRecord updatedRecord;
        if (response is not null && !string.IsNullOrWhiteSpace(response.EmergencyIncidentId))
        {
            updatedRecord = new ForwardingRecord(
                ForwardingId: existing?.ForwardingId ?? Guid.NewGuid(),
                CallId: request.CallId,
                TargetService: request.TargetService,
                Status: "Forwarded",
                ExternalReferenceId: response.EmergencyIncidentId,
                ErrorMessage: null,
                AttemptedAtUtc: DateTime.UtcNow,
                AttemptCount: attemptNumber
            );
        }
        else
        {
            updatedRecord = new ForwardingRecord(
                ForwardingId: existing?.ForwardingId ?? Guid.NewGuid(),
                CallId: request.CallId,
                TargetService: request.TargetService,
                Status: "Failed",
                ExternalReferenceId: null,
                ErrorMessage: "Emergency service rejected the request or was unreachable.",
                AttemptedAtUtc: DateTime.UtcNow,
                AttemptCount: attemptNumber
            );
        }

        _recordsByCallId[request.CallId] = updatedRecord;
        return updatedRecord;
    }

    public IReadOnlyList<ForwardingRecord> GetForwardingHistory(int? callId = null)
    {
        var query = _recordsByCallId.Values.AsEnumerable();
        if (callId.HasValue)
        {
            query = query.Where(r => r.CallId == callId.Value);
        }
        return query.OrderByDescending(r => r.AttemptedAtUtc).ToList();
    }
}
