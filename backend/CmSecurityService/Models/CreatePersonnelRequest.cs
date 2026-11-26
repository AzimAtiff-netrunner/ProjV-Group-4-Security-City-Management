using System.ComponentModel.DataAnnotations;

namespace CmSecurityService.Models;

public class CreatePersonnelRequest
{
    // Optional: supplied once Registration has created the user account.
    [StringLength(36)]
    public string? UserId { get; set; }

    [Required]
    [StringLength(30)]
    public string BadgeNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? RankTitle { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    public DateOnly? HireDate { get; set; }

    [Required]
    public string RoleName { get; set; } = string.Empty;
}