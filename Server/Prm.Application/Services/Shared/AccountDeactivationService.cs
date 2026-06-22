using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Shared;

public class AccountDeactivationService : IAccountDeactivationService
{
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly IUserRepository _users;
    private readonly IProjectRepository _projects;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AccountDeactivationService> _logger;

    public AccountDeactivationService(
        IResourceProfileRepository resourceProfiles,
        IUserRepository users,
        IProjectRepository projects,
        IAuditLogService auditLog,
        ILogger<AccountDeactivationService> logger)
    {
        _resourceProfiles = resourceProfiles;
        _users = users;
        _projects = projects;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<DeactivateEmployeeResponse> DeactivateEmployeeAsync(
        ResourceProfile resourceProfile,
        User user,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        UserGuard.EnsureNotSelfDeactivation(actingUserId, resourceProfile.UserId);
        EmployeeGuard.EnsureNotAlreadyInactive(resourceProfile);
        await ValidateCanDeactivateUserAsync(user, cancellationToken);

        var oldUserSnapshot = AuditSnapshotBuilder.UserSnapshot(user);
        var oldProfileSnapshot = AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile);

        var today = ActiveDateHelper.TodayUtc;
        var activeAllocations = await _resourceProfiles.GetActiveAllocationsAsync(resourceProfile.Id, cancellationToken);
        await _resourceProfiles.EndActiveAllocationsAsync(resourceProfile.Id, today, cancellationToken);

        await ApplyDeactivationAsync(user, resourceProfile, cancellationToken);

        var updatedUser = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(user.Id, cancellationToken),
            ErrorMessages.UserNotFound);
        var updatedProfile = EntityGuard.EnsureFound(
            await _resourceProfiles.GetByIdAsync(resourceProfile.Id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var adminRole = AuthConstants.RoleName(UserRole.Admin);
        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.User,
            updatedUser.Id,
            AuditConstants.Actions.Deactivated,
            oldUserSnapshot,
            AuditSnapshotBuilder.UserSnapshot(updatedUser),
            actingUserId,
            adminRole,
            AuditConstants.Sources.User,
            cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.ResourceProfile,
            updatedProfile.Id,
            AuditConstants.Actions.Deactivated,
            oldProfileSnapshot,
            AuditSnapshotBuilder.ResourceProfileSnapshot(updatedProfile),
            actingUserId,
            adminRole,
            AuditConstants.Sources.User,
            cancellationToken);

        _logger.LogInformation(
            "Employee deactivated. ResourceProfileId={ResourceProfileId}, UserId={UserId}, AllocationsEnded={AllocationCount}",
            resourceProfile.Id,
            user.Id,
            activeAllocations.Count);

        return new DeactivateEmployeeResponse(
            $"{user.FullName} deactivated. Active allocations ended.",
            activeAllocations.Count);
    }

    public async Task DeactivateUserAsync(User user, int actingUserId, CancellationToken cancellationToken = default)
    {
        UserGuard.EnsureNotSelfDeactivation(actingUserId, user.Id);
        UserAvailabilityGuard.EnsureNotAlreadyInactive(user.IsActive);

        await ValidateCanDeactivateUserAsync(user, cancellationToken);

        var linkedProfile = await _resourceProfiles.GetByUserIdAsync(user.Id, cancellationToken);
        await ApplyDeactivationAsync(user, linkedProfile, cancellationToken);
    }

    private async Task ValidateCanDeactivateUserAsync(User user, CancellationToken cancellationToken)
    {
        var role = UserRoleHelper.GetPrimaryRole(user);

        if (role == UserRole.Admin)
        {
            var activeAdminCount = await _users.CountActiveAdminsAsync(cancellationToken: cancellationToken);
            UserGuard.EnsureNotLastActiveAdmin(activeAdminCount);
        }

        if (role != UserRole.Manager)
        {
            return;
        }

        var hasTeam = await _resourceProfiles.HasActiveTeamMembersAsync(user.Id, cancellationToken);
        var hasProjects = await _projects.HasActiveProjectsForManagerAsync(user.Id, cancellationToken);
        ManagerDeactivationGuard.EnsureCanDeactivateManager(hasTeam, hasProjects);
    }

    private async Task ApplyDeactivationAsync(User user, ResourceProfile? resourceProfile, CancellationToken cancellationToken)
    {
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        if (resourceProfile is null)
        {
            return;
        }

        resourceProfile.ResourceStatus = DomainDefaults.NewResourceStatus;
        resourceProfile.UpdatedAt = DateTime.UtcNow;
        await _resourceProfiles.UpdateAsync(resourceProfile, cancellationToken);
    }
}
