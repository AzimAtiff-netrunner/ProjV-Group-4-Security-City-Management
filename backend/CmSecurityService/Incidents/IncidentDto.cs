namespace CmSecurityService.Incidents;

/// <summary>
/// Public view of a police incident returned by the View Incidents API.
/// </summary>
/// <remarks>
/// This is a DTO (Data Transfer Object): it only contains fields that are safe to
/// share with callers. Internal fields such as officer notes or soft-delete flags
/// stay in the storage model and are never copied here.
/// </remarks>
public class IncidentDto
{
    /// <summary>Unique incident identifier.</summary>
    public int Id { get; set; }

    /// <summary>Short headline of the incident.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Full description of what was reported.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Category of incident, e.g. "Theft", "Vandalism", "Traffic".</summary>
    public string IncidentType { get; set; } = string.Empty;

    /// <summary>Current status as text: Open, UnderInvestigation or Closed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Neighbourhood or area where the incident happened.</summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>When the incident was reported (UTC).</summary>
    public DateTime ReportedAt { get; set; }
}
