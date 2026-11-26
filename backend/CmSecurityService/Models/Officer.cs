using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmSecurityService.Models;

[Table("officer")]
public class Officer
{
    [Key]
    [Column("officer_id", TypeName = "char")]
    [MaxLength(36)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string OfficerId { get; set; } = string.Empty;

    [Column("user_id", TypeName = "char")]
    [MaxLength(36)]
    public string? UserId { get; set; }

    [Column("badge_number")]
    public string BadgeNumber { get; set; } = string.Empty;

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("rank_title")]
    public string? RankTitle { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("hire_date", TypeName = "date")]
    public DateOnly? HireDate { get; set; }

    [Column("is_active", TypeName = "tinyint(1)")]
    public bool IsActive { get; set; } = true;

    [Column("created_at", TypeName = "datetime(6)")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; set; }
}