namespace CmSecurityService.Models;

public class PersonnelResponse
{
    public string OfficerId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string BadgeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? RankTitle { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? HireDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? CurrentRole { get; set; }
}