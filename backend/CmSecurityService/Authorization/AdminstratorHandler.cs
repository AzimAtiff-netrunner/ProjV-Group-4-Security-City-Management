using CmSecurityService.Autorization;
using CmSecurityService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Authorization;

public class SecurityAdministratorHandler : AuthorizationHandler<AdministratorRequirement>
{
    private readonly AppDbContext _context;

    public SecurityAdministratorHandler(AppDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdministratorRequirement requirement)
    {
        // Registration puts the user's ID in the JWT subclaim.
        string? userId = context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return;

        // Check whether this user is an active officer
        // with an active Security Administrator assignment.
        bool isSecurityAdministrator =
            await (
                from officer in _context.Officers
                join officerRole in _context.OfficerRoles
                    on officer.OfficerId equals officerRole.OfficerId
                join role in _context.Roles
                    on officerRole.RoleId equals role.RoleId
                where officer.UserId == userId
                    && officer.IsActive
                    && officerRole.RevokedAt == null
                    && role.Name == "Security Administrator"
                select officer.OfficerId
            ).AnyAsync();

        if (isSecurityAdministrator)
        {
            context.Succeed(requirement);
        }
    }
}