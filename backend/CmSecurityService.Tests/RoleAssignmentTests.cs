using CmSecurityService.Controllers;
using CmSecurityService.Data;
using CmSecurityService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Tests;

public class RoleAssignmentTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Officer CreateOfficer(string officerId, bool isActive = true)
    {
        return new Officer
        {
            OfficerId = officerId,
            BadgeNumber = "12345",
            FirstName = "John",
            LastName = "Smith",
            IsActive = isActive
        };
    }

    private static Role CreateRole(uint roleId, string name)
    {
        return new Role
        {
            RoleId = roleId,
            Name = name
        };
    }

    // Assigns a role successfully.
    [Fact]
    public async Task AssignRole_ValidRequest_ReturnsOk()
    {
        using var context = CreateContext();

        const string officerId = "officer-123";

        context.Officers.Add(CreateOfficer(officerId));
        context.Roles.Add(CreateRole(1, "Sergeant"));
        await context.SaveChangesAsync();

        var controller = new PersonnelController(context);

        var result = await controller.AssignRole(
            officerId,
            new AssignRoleRequest { RoleName = "Sergeant" });

        Assert.IsType<OkObjectResult>(result);

        var assignment = await context.OfficerRoles
            .SingleAsync();

        Assert.Equal(officerId, assignment.OfficerId);
        Assert.Equal(1u, assignment.RoleId);
        Assert.Null(assignment.RevokedAt);
        Assert.False(string.IsNullOrWhiteSpace(assignment.OfficerRoleId));
    }

    // Reassigns an officer who already has a role.
    [Fact]
    public async Task AssignRole_ExistingRole_RevokesOldAndCreatesNewAssignment()
    {
        using var context = CreateContext();

        const string officerId = "officer-123";

        context.Officers.Add(CreateOfficer(officerId));

        context.Roles.AddRange(
            CreateRole(1, "Police Officer"),
            CreateRole(2, "Sergeant"));

        context.OfficerRoles.Add(new OfficerRole
        {
            OfficerRoleId = Guid.NewGuid().ToString(),
            OfficerId = officerId,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow,
            RevokedAt = null
        });

        await context.SaveChangesAsync();

        var controller = new PersonnelController(context);

        var result = await controller.AssignRole(
            officerId,
            new AssignRoleRequest { RoleName = "Sergeant" });

        Assert.IsType<OkObjectResult>(result);

        var assignments = await context.OfficerRoles
            .OrderBy(r => r.RoleId)
            .ToListAsync();

        Assert.Equal(2, assignments.Count);

        var oldAssignment = assignments.Single(r => r.RoleId == 1);
        var newAssignment = assignments.Single(r => r.RoleId == 2);

        Assert.NotNull(oldAssignment.RevokedAt);
        Assert.Null(newAssignment.RevokedAt);
    }

    // if an Officer does not exist.
    [Fact]
    public async Task AssignRole_OfficerDoesNotExist_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new PersonnelController(context);

        var result = await controller.AssignRole(
            "missing-officer",
            new AssignRoleRequest { RoleName = "Sergeant" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // if Officer is inactive.
    [Fact]
    public async Task AssignRole_InactiveOfficer_ReturnsConflict()
    {
        using var context = CreateContext();

        const string officerId = "officer-123";

        context.Officers.Add(CreateOfficer(officerId, false));
        await context.SaveChangesAsync();

        var controller = new PersonnelController(context);

        var result = await controller.AssignRole(
            officerId,
            new AssignRoleRequest { RoleName = "Sergeant" });

        Assert.IsType<ConflictObjectResult>(result);

        Assert.Empty(await context.OfficerRoles.ToListAsync());
    }

    //Invalid role name.
    [Fact]
    public async Task AssignRole_InvalidRoleName_ReturnsBadRequest()
    {
        using var context = CreateContext();

        const string officerId = "officer-123";

        context.Officers.Add(CreateOfficer(officerId));
        await context.SaveChangesAsync();

        var controller = new PersonnelController(context);

        var result = await controller.AssignRole(
            officerId,
            new AssignRoleRequest { RoleName = "Manager" });

        Assert.IsType<BadRequestObjectResult>(result);

        Assert.Empty(await context.OfficerRoles.ToListAsync());
    }

    // Valid role name but role is missing from database.
    [Fact]
    public async Task AssignRole_RoleMissingFromDatabase_ReturnsNotFound()
    {
        using var context = CreateContext();

        const string officerId = "officer-123";

        context.Officers.Add(CreateOfficer(officerId));
        await context.SaveChangesAsync();

        var controller = new PersonnelController(context);

        var result = await controller.AssignRole(
            officerId,
            new AssignRoleRequest { RoleName = "Sergeant" });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // GET personnel includes the current assigned role.
    [Fact]
    public async Task GetPersonnel_OfficerHasActiveRole_ReturnsCurrentRole()
    {
        using var context = CreateContext();

        const string officerId = "officer-123";

        context.Officers.Add(CreateOfficer(officerId));
        context.Roles.Add(CreateRole(1, "Sergeant"));

        context.OfficerRoles.Add(new OfficerRole
        {
            OfficerRoleId = Guid.NewGuid().ToString(),
            OfficerId = officerId,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow,
            RevokedAt = null
        });

        await context.SaveChangesAsync();

        var controller = new PersonnelController(context);

        var result = await controller.GetPersonnel();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var officers = Assert.IsType<List<PersonnelResponse>>(okResult.Value);

        var officer = Assert.Single(officers);

        Assert.Equal("Sergeant", officer.CurrentRole);
    }

    // GET personnel returns null when there is no active role.
    [Fact]
    public async Task GetPersonnel_NoActiveRole_ReturnsNullCurrentRole()
    {
        using var context = CreateContext();

        const string officerId = "officer-123";

        context.Officers.Add(CreateOfficer(officerId));
        context.Roles.Add(CreateRole(1, "Police Officer"));

        context.OfficerRoles.Add(new OfficerRole
        {
            OfficerRoleId = Guid.NewGuid().ToString(),
            OfficerId = officerId,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow,
            RevokedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var controller = new PersonnelController(context);

        var result = await controller.GetPersonnel();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var officers = Assert.IsType<List<PersonnelResponse>>(okResult.Value);

        var officer = Assert.Single(officers);

        Assert.Null(officer.CurrentRole);
    }
}