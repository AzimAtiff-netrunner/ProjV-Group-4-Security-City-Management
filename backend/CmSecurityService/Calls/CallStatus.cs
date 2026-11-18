namespace CmSecurityService.Calls;

/// <summary>
/// Lifecycle status of a 9-1-1 call.
/// </summary>
/// <remarks>
/// Proposed values; must match the call_status table and the values set by
/// "Forward 9-1-1 calls" once the team agrees on them.
/// </remarks>
public enum CallStatus
{
    /// <summary>Call received by Security; not yet handled.</summary>
    Received,

    /// <summary>Officers are responding to the call.</summary>
    InProgress,

    /// <summary>Call forwarded to Emergency Services (Fire/EMS).</summary>
    Forwarded,

    /// <summary>Forwarding was attempted and failed.</summary>
    Failed,

    /// <summary>Call handled; no further action.</summary>
    Closed
}
