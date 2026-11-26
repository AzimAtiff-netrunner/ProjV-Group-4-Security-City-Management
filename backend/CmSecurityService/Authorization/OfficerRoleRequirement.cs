using Microsoft.AspNetCore.Authorization;

namespace CmSecurityService.Authorization;

/// <summary>
/// Requires the caller to be an active officer who currently holds at least one of the listed roles.
/// </summary>
public class OfficerRoleRequirement : IAuthorizationRequirement
{
    /// <summary>Role names that satisfy the requirement.</summary>
    public IReadOnlyCollection<string> AllowedRoles { get; }

    /// <summary>Creates the requirement.</summary>
    /// <param name="allowedRoles">Role names (as stored in the role table) that are allowed.</param>
    public OfficerRoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles;
    }
}
