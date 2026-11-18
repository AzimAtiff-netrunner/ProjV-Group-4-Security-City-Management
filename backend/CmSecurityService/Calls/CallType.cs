namespace CmSecurityService.Calls;

/// <summary>
/// Which service a 9-1-1 call needs.
/// </summary>
public enum CallType
{
    /// <summary>Handled by Security (police).</summary>
    Police,

    /// <summary>Needs the fire department.</summary>
    Fire,

    /// <summary>Needs emergency medical services.</summary>
    EMS
}
