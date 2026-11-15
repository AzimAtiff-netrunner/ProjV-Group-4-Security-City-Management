using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CmSecurityService.Models;

[Table("role")]
public class Role
{
    [Key]
    [Column("role_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public uint RoleId { get; set; }

    [Column("name")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }
}