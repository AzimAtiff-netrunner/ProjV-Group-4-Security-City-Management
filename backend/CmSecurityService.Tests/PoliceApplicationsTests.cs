using System.Security.Claims;
using CmSecurityService.Applications;
using CmSecurityService.Authorization;
using CmSecurityService.Common;
using CmSecurityService.Controllers;
using CmSecurityService.Data;
using CmSecurityService.Incidents;
using CmSecurityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CmSecurityService.Tests;

public class PoliceApplicationsTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ApplicationsController CreateController(AppDbContext context, string? userId = "admin-user-1")
    {
        var service = new EfPoliceApplicationService(context, TimeProvider.System);
        var controller = new ApplicationsController(service);

        var identity = string.IsNullOrWhiteSpace(userId)
            ? new ClaimsIdentity()
            : new ClaimsIdentity([new Claim("sub", userId)], "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };

        return controller;
    }

    private static PoliceApplication CreateApplication(
        Guid? id = null,
        string firstName = "Marcus",
        string lastName = "Vance",
        ApplicationStatus status = ApplicationStatus.Pending,
        DateTime? submittedAt = null)
    {
        return new PoliceApplication
        {
            ApplicationId = id ?? Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.ca",
            Phone = "519-555-0141",
            DateOfBirth = new DateOnly(1997, 4, 18),
            Address = "120 King Street North, Waterloo, ON",
            Qualifications = "Police Foundations Diploma",
            ExperienceYears = 2,
            Status = status,
            SubmittedAt = submittedAt ?? DateTime.UtcNow
        };
    }

    // AC 1 & AC 2: Retrieve a list of pending police applications with relevant applicant details
    [Fact]
    public async Task GetApplications_DefaultQuery_ReturnsOnlyPendingApplicationsWithApplicantDetails()
    {
        using var context = CreateContext();
        var pendingId = Guid.NewGuid();

        context.PoliceApplications.AddRange(
            CreateApplication(pendingId, "Marcus", "Vance", ApplicationStatus.Pending),
            CreateApplication(Guid.NewGuid(), "Devon", "Brooks", ApplicationStatus.Approved),
            CreateApplication(Guid.NewGuid(), "Sara", "Chen", ApplicationStatus.Rejected));
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetApplications(new ApplicationQuery(), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var page = Assert.IsType<PagedResult<PoliceApplicationDto>>(okResult.Value);

        Assert.Equal(1, page.TotalCount);
        var item = Assert.Single(page.Items);
        Assert.Equal(pendingId, item.ApplicationId);
        Assert.Equal("Marcus", item.FirstName);
        Assert.Equal("Vance", item.LastName);
        Assert.Equal("marcus.vance@example.ca", item.Email);
        Assert.Equal("120 King Street North, Waterloo, ON", item.Address);
        Assert.Equal("Police Foundations Diploma", item.Qualifications);
        Assert.Equal(2u, item.ExperienceYears);
        Assert.Equal("Pending", item.Status);
    }

    // AC 3: Security Administrators can view an individual application
    [Fact]
    public async Task GetApplicationById_ExistingId_ReturnsApplicationDetails()
    {
        using var context = CreateContext();
        var appId = Guid.NewGuid();

        context.PoliceApplications.Add(CreateApplication(appId, "Elena", "Rostova", ApplicationStatus.Pending));
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetApplicationById(appId, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PoliceApplicationDto>(okResult.Value);

        Assert.Equal(appId, dto.ApplicationId);
        Assert.Equal("Elena", dto.FirstName);
        Assert.Equal("Rostova", dto.LastName);
        Assert.Equal("Pending", dto.Status);
    }

    // AC 4 & AC 6: Approve an application and record updated status
    [Fact]
    public async Task ApproveApplication_PendingApplication_UpdatesStatusAndRecordsReviewer()
    {
        using var context = CreateContext();
        var appId = Guid.NewGuid();
        const string adminUserId = "user-admin-1";
        const string adminOfficerId = "officer-admin-1";

        context.Officers.Add(new Officer
        {
            OfficerId = adminOfficerId,
            UserId = adminUserId,
            BadgeNumber = "9001",
            FirstName = "Ayman",
            LastName = "Ahmed",
            IsActive = true
        });
        context.PoliceApplications.Add(CreateApplication(appId, "Marcus", "Vance", ApplicationStatus.Pending));
        await context.SaveChangesAsync();

        var controller = CreateController(context, adminUserId);

        var result = await controller.ApproveApplication(
            appId,
            new ReviewApplicationRequest("Meets all qualifications."),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PoliceApplicationDto>(okResult.Value);

        Assert.Equal("Approved", dto.Status);
        Assert.Equal("Meets all qualifications.", dto.ReviewerNotes);
        Assert.Equal(adminOfficerId, dto.ReviewedByOfficerId);
        Assert.NotNull(dto.ReviewedAt);

        var persisted = await context.PoliceApplications.SingleAsync(a => a.ApplicationId == appId);
        Assert.Equal(ApplicationStatus.Approved, persisted.Status);
        Assert.NotNull(persisted.ReviewedAt);
    }

    // AC 5 & AC 6: Reject an application and record updated status
    [Fact]
    public async Task RejectApplication_PendingApplication_UpdatesStatusAndRecordsReason()
    {
        using var context = CreateContext();
        var appId = Guid.NewGuid();

        context.PoliceApplications.Add(CreateApplication(appId, "Marcus", "Vance", ApplicationStatus.Pending));
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.RejectApplication(
            appId,
            new ReviewApplicationRequest("Incomplete background documentation."),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PoliceApplicationDto>(okResult.Value);

        Assert.Equal("Rejected", dto.Status);
        Assert.Equal("Incomplete background documentation.", dto.ReviewerNotes);
        Assert.NotNull(dto.ReviewedAt);

        var persisted = await context.PoliceApplications.SingleAsync(a => a.ApplicationId == appId);
        Assert.Equal(ApplicationStatus.Rejected, persisted.Status);
    }

    // AC 7: Prevent an application from being reviewed repeatedly when already finalized
    [Theory]
    [InlineData(ApplicationStatus.Approved)]
    [InlineData(ApplicationStatus.Rejected)]
    public async Task ReviewApplication_AlreadyFinalized_ReturnsConflict(ApplicationStatus existingStatus)
    {
        using var context = CreateContext();
        var appId = Guid.NewGuid();

        context.PoliceApplications.Add(CreateApplication(appId, "Devon", "Brooks", existingStatus));
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var approveResult = await controller.ApproveApplication(appId, new ReviewApplicationRequest("Retry"), CancellationToken.None);
        var approveConflict = Assert.IsType<ConflictObjectResult>(approveResult);
        var approveError = Assert.IsType<ApiError>(approveConflict.Value);
        Assert.Equal("application_already_finalized", approveError.Error);

        var rejectResult = await controller.RejectApplication(appId, new ReviewApplicationRequest("Retry"), CancellationToken.None);
        var rejectConflict = Assert.IsType<ConflictObjectResult>(rejectResult);
        var rejectError = Assert.IsType<ApiError>(rejectConflict.Value);
        Assert.Equal("application_already_finalized", rejectError.Error);
    }

    // AC 9: Handle nonexistent application IDs appropriately
    [Fact]
    public async Task Endpoints_NonexistentApplicationId_ReturnsNotFoundWithApiError()
    {
        using var context = CreateContext();
        var missingId = Guid.NewGuid();
        var controller = CreateController(context);

        var getResult = await controller.GetApplicationById(missingId, CancellationToken.None);
        var getNotFound = Assert.IsType<NotFoundObjectResult>(getResult);
        Assert.Equal("application_not_found", Assert.IsType<ApiError>(getNotFound.Value).Error);

        var approveResult = await controller.ApproveApplication(missingId, null, CancellationToken.None);
        var approveNotFound = Assert.IsType<NotFoundObjectResult>(approveResult);
        Assert.Equal("application_not_found", Assert.IsType<ApiError>(approveNotFound.Value).Error);

        var rejectResult = await controller.RejectApplication(missingId, null, CancellationToken.None);
        var rejectNotFound = Assert.IsType<NotFoundObjectResult>(rejectResult);
        Assert.Equal("application_not_found", Assert.IsType<ApiError>(rejectNotFound.Value).Error);
    }

    // AC 8: Unauthorized users cannot approve or reject applications
    [Fact]
    public void ApplicationsController_RequiresSecurityAdministratorPolicy()
    {
        var authorizeAttr = typeof(ApplicationsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(authorizeAttr);
        Assert.Equal("SecurityAdministrator", authorizeAttr.Policy);
    }

    [Fact]
    public async Task SecurityAdministratorHandler_NonAdminUser_DoesNotSucceed()
    {
        using var context = CreateContext();
        const string patrolUserId = "patrol-user-1";
        const string officerId = "officer-patrol-1";

        context.Officers.Add(new Officer
        {
            OfficerId = officerId,
            UserId = patrolUserId,
            BadgeNumber = "2001",
            FirstName = "Patrol",
            LastName = "Officer",
            IsActive = true
        });
        context.Roles.Add(new Role { RoleId = 1, Name = "Police Officer" });
        context.OfficerRoles.Add(new OfficerRole
        {
            OfficerRoleId = Guid.NewGuid().ToString(),
            OfficerId = officerId,
            RoleId = 1,
            AssignedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var requirement = new AdministratorRequirement();
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", patrolUserId)], "TestAuth"));
        var authContext = new AuthorizationHandlerContext([requirement], user, resource: null);

        var handler = new SecurityAdministratorHandler(context);
        await handler.HandleAsync(authContext);

        Assert.False(authContext.HasSucceeded);
    }
}