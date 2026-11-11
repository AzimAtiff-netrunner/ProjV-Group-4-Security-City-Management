namespace CmSecurityService.Models;

public class Officer
{
    public string OfficerId {get; set; } = string.Empty; //officer unique id
    public string? UserId { get; set; }
    public string BadgeNumber { get; set; } = string.Empty; //required info
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? RankTitle { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    //public DateOnly? HireDate { get; set; } 
    public bool IsActive { get; set; } = true; //to tell if an offcier is active or not
    public DateTime CreatedAt { get; set; } // when officer was created
}