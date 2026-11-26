using CmSecurityService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Authorization;

/// <summary>
/// Succeeds when the JWT's user belongs to an active officer with a current (not revoked)
/// role listed in the requirement. Works the same way as <see cref="SecurityAdministratorHandler"/>.
/// </summary>
public class OfficerRoleHandler : AuthorizationHandler<OfficerRoleRequirement>
{
    private readonly AppDbContext _context;

    /// <summary>Creates the handler.</summary>
    /// <param name="context">Database context.</param>
    public OfficerRoleHandler(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OfficerRoleRequirement requirement)
    {
        // Registration puts the user's ID in the JWT "sub" claim.
        string? userId = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var allowedRoles = requirement.AllowedRoles.ToList();

        bool allowed =
            await (
                from officer in _context.Officers
                join officerRole in _context.OfficerRoles
                    on officer.OfficerId equals officerRole.OfficerId
                join role in _context.Roles
                    on officerRole.RoleId equals role.RoleId
                where officer.UserId == userId
                    && officer.IsActive
                    && officerRole.RevokedAt == null
                    && allowedRoles.Contains(role.Name)
                select officer.OfficerId
            ).AnyAsync();

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}
