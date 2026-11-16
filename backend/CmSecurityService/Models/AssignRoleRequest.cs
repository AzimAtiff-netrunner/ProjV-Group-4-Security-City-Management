using System.ComponentModel.DataAnnotations;
namespace CmSecurityService.Models;

public class AssignRoleRequest
{
    [Required]
    public string RoleName {get; set;} = string.Empty;
}