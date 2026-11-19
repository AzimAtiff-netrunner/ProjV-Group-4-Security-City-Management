namespace CmSecurityService.Controllers;
using CmSecurityService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CmSecurityService.Models;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/security/v1/personnel")]
public class PersonnelController : ControllerBase
{
    private readonly AppDbContext _context; //access to db through EF core

    public PersonnelController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<PersonnelResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPersonnel()
    {
        var officers = await _context.Officers.Select(o => new PersonnelResponse
            {
                OfficerId = o.OfficerId,
                UserId = o.UserId,
                BadgeNumber = o.BadgeNumber,
                FirstName = o.FirstName,
                LastName = o.LastName,
                RankTitle = o.RankTitle,
                Phone = o.Phone,
                Email = o.Email,
                HireDate = o.HireDate,
                IsActive = o.IsActive,
                CreatedAt = o.CreatedAt,

                CurrentRole = _context.OfficerRoles.Where(or => or.OfficerId == o.OfficerId && or.RevokedAt == null)
                    .Join(_context.Roles, or => or.RoleId, r => r.RoleId, (or, r) => r.Name).FirstOrDefault()}).ToListAsync();

        return Ok(officers);
    }
    [HttpPut("{officerId}/role")]
    [Authorize(Policy = "SecurityAdministrator")]
    public async Task<IActionResult> AssignRole(string officerId,[FromBody] AssignRoleRequest request)
    {
    // Check whether the officer exists.
    var officer = await _context.Officers.FirstOrDefaultAsync(o => o.OfficerId == officerId);

    if (officer == null)
        return NotFound("Officer not found.");

    // Only active officers can receive role assignments.
    if (!officer.IsActive)
        return Conflict("Cannot assign a role to an inactive officer.");

    var allowedRoles = new[]
    {
        "Police Officer",
        "Sergeant",
        "Police Chief",
        "Security Administrator"
    };

    var requestedRole = allowedRoles.FirstOrDefault(r => r.Equals(request.RoleName?.Trim(), StringComparison.OrdinalIgnoreCase));

    if (requestedRole == null)
        return BadRequest("Invalid role name.");

    // Find the role in the database.
    var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == requestedRole);

    if (role == null)
        return NotFound("Role does not exist in the database.");

    // Start a transaction when using a relational database.
    Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

    if (_context.Database.IsRelational())
        transaction = await _context.Database.BeginTransactionAsync();

    try
    {
    // Find the officer's current active role assignment.
        var currentAssignment = await _context.OfficerRoles.FirstOrDefaultAsync(r => r.OfficerId == officerId && r.RevokedAt == null);
        // Revoke the previous assignment.
        if (currentAssignment != null) {
            currentAssignment.RevokedAt = DateTime.UtcNow;
        }
        // Create the new assignment.
        var newAssignment = new OfficerRole {
            OfficerId = officerId,
            RoleId = role.RoleId
        };

        _context.OfficerRoles.Add(newAssignment);

        await _context.SaveChangesAsync();

        if (transaction != null)
            await transaction.CommitAsync();

        return Ok(new
        {
            officerId = officerId,
            roleName = role.Name,
            message = "Role assigned successfully."
        });
    }
    catch
    {
        if (transaction != null)
            await transaction.RollbackAsync();
            throw;
    }
    finally
    {
        if (transaction != null)
            await transaction.DisposeAsync();
        }
    }
}