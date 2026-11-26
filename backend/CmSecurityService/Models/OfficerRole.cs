using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmSecurityService.Models;

[Table("officer_role")]
public class OfficerRole
{
    [Key]
    [Column("officer_role_id", TypeName = "char")]
    [MaxLength(36)]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string OfficerRoleId { get; set; } = Guid.NewGuid().ToString();

    [Column("officer_id", TypeName = "char")]
    [MaxLength(36)]
    public string OfficerId { get; set; } = string.Empty;

    [Column("role_id")]
    public uint RoleId { get; set; }

    [Column("assigned_at", TypeName = "datetime(6)")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime AssignedAt { get; set; }

    [Column("revoked_at", TypeName = "datetime(6)")]
    public DateTime? RevokedAt { get; set; }
}