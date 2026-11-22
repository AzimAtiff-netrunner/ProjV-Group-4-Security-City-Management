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

    [HttpPost]
    [Authorize(Policy = "SecurityAdministrator")]
    [ProducesResponseType(typeof(PersonnelResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePersonnel(
        [FromBody] CreatePersonnelRequest request)
    {
        // Check that the requested role is one of our approved roles.
        var allowedRoles = new[]
        {
            "Police Officer",
            "Sergeant",
            "Police Chief",
            "Security Administrator"
        };

        var requestedRole = allowedRoles.FirstOrDefault(
            r => r.Equals(request.RoleName?.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (requestedRole == null)
            return BadRequest("Invalid role name.");

        // Check that the role exists in the database.
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == requestedRole);

        if (role == null)
            return BadRequest("The requested role does not exist in the database.");

        // Check for an existing badge number.
        var badgeNumber = request.BadgeNumber.Trim();

        bool badgeExists = await _context.Officers.AnyAsync(o => o.BadgeNumber == badgeNumber);

        if (badgeExists)
            return Conflict("A personnel record with this badge number already exists.");

        // Create the officer record.
        var officer = new Officer
        {
            OfficerId = Guid.NewGuid().ToString(),
            UserId = string.IsNullOrWhiteSpace(request.UserId)
                ? null
                : request.UserId.Trim(),
            BadgeNumber = badgeNumber,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            RankTitle = request.RankTitle?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            HireDate = request.HireDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Create the initial role assignment.
        var officerRole = new OfficerRole
        {
            OfficerId = officer.OfficerId,
            RoleId = role.RoleId
        };

        // Save both records together.
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        if (_context.Database.IsRelational())
            transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.Officers.Add(officer);
            _context.OfficerRoles.Add(officerRole);

            await _context.SaveChangesAsync();

            if (transaction != null)
                await transaction.CommitAsync();

            var response = new PersonnelResponse
            {
                OfficerId = officer.OfficerId,
                UserId = officer.UserId,
                BadgeNumber = officer.BadgeNumber,
                FirstName = officer.FirstName,
                LastName = officer.LastName,
                RankTitle = officer.RankTitle,
                Phone = officer.Phone,
                Email = officer.Email,
                HireDate = officer.HireDate,
                IsActive = officer.IsActive,
                CreatedAt = officer.CreatedAt,
                CurrentRole = role.Name
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (DbUpdateException)
        {
            if (transaction != null)
                await transaction.RollbackAsync();

            return Conflict(
                "The personnel record could not be created. Check that the badge number and user ID are not already assigned and that the user ID exists.");
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