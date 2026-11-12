namespace CmSecurityService.Incidents;

/// <summary>
/// Lifecycle status of a police incident.
/// </summary>
/// <remarks>
/// Must stay in sync with the values written by "Register police incidents" (#217).
/// </remarks>
public enum IncidentStatus
{
    /// <summary>Reported but no investigation started yet.</summary>
    Open,

    /// <summary>Actively being investigated by an officer.</summary>
    UnderInvestigation,

    /// <summary>Investigation finished; no further action.</summary>
    Closed
}
