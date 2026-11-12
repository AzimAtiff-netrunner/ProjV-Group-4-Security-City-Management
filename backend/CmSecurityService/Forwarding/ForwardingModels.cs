using System.ComponentModel.DataAnnotations;

namespace CmSecurityService.Forwarding;

public record EmergencyIncidentPayload(
    [Required] string ExternalSource,
    [Required] string IncidentType,
    [Required] string Description,
    [Required] string Location,
    [Required] string Priority,
    DateTime ReportedAtUtc,
    string? IdempotencyKey
);

public record EmergencyServiceResponse(
    string EmergencyIncidentId,
    string Status,
    DateTime AcknowledgedAtUtc
);

public record ForwardCallRequest(
    [Required] int CallId,
    [Required] string TargetService,
    string? DispatcherNotes
);

public record ForwardingRecord(
    Guid ForwardingId,
    int CallId,
    string TargetService,
    string Status,
    string? ExternalReferenceId,
    string? ErrorMessage,
    DateTime AttemptedAtUtc,
    int AttemptCount
);
