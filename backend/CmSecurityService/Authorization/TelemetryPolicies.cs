namespace CmSecurityService.Authorization;

/// <summary>
/// Policy names and role sets for vehicle telemetry.
/// </summary>
public static class TelemetryPolicies
{
    /// <summary>Who may read vehicles and telemetry.</summary>
    public const string Viewer = "TelemetryViewer";

    /// <summary>Who may record telemetry. Applied on top of <see cref="Viewer"/>.</summary>
    public const string Writer = "TelemetryWriter";

    /// <summary>Who may register new vehicles. Applied on top of <see cref="Viewer"/>.</summary>
    public const string Manager = "VehicleManager";

    /// <summary>Roles allowed to read: every officer role.</summary>
    public static readonly string[] ViewerRoles =
    {
        "Police Officer",
        "Sergeant",
        "Police Chief",
        "Security Administrator"
    };

    /// <summary>Roles allowed to record updates.</summary>
    public static readonly string[] WriterRoles =
    {
        "Security Administrator"
    };

    /// <summary>Roles allowed to register vehicles.</summary>
    public static readonly string[] ManagerRoles =
    {
        "Security Administrator"
    };
}
