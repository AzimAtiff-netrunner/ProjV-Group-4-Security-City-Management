using CmSecurityService.Authorization;
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
        Console.WriteLine($"JWT sub claim: {userId}");


        if (string.IsNullOrWhiteSpace(userId))
            return;

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