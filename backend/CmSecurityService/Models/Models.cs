namespace CmSecurityService.Models;

public class IncidentType
{
    public uint IncidentTypeId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class IncidentStatus
{
    public uint IncidentStatusId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
}

public class Incident
{
    public Guid IncidentId { get; set; }
    public uint IncidentTypeId { get; set; }
    public string Description { get; set; } = "";
    public uint IncidentStatusId { get; set; }
    public DateTime CreatedAt { get; set; }

    public IncidentType? IncidentType { get; set; }
    public IncidentStatus? IncidentStatus { get; set; }
}