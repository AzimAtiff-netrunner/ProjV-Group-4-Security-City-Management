namespace CmSecurityService.Calls;

/// <summary>
/// Public view of a 9-1-1 call returned by the View 9-1-1 Calls API.
/// </summary>
/// <remarks>
/// Only fields a dispatcher needs. Internal fields such as operator notes stay in the
/// storage model and are never copied here.
/// </remarks>
public class CallDto
{
    /// <summary>Unique call identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Caller's name, if given.</summary>
    public string? CallerName { get; set; }

    /// <summary>Caller's phone number, so a dispatcher can call back.</summary>
    public string? CallerPhone { get; set; }

    /// <summary>Where the emergency is.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>Police, Fire or EMS.</summary>
    public string CallType { get; set; } = string.Empty;

    /// <summary>What the caller reported.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Received, Forwarded, Failed or Closed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>When the call was received (UTC).</summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>When the call was forwarded to Emergency Services (UTC), if it was.</summary>
    public DateTime? ForwardedAt { get; set; }
}
