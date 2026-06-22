using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Admin.Users;

public class AdminUserLifecycleService
{
    private readonly IUserRepository _users;
    private readonly IResourceProfileRepository _resourceProfiles;
    private readonly IAccountDeactivationService _accountDeactivation;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AdminUserLifecycleService> _logger;

    public AdminUserLifecycleService(
        IUserRepository users,
        IResourceProfileRepository resourceProfiles,
        IAccountDeactivationService accountDeactivation,
        IAuditLogService auditLog,
        ILogger<AdminUserLifecycleService> logger)
    {
        _users = users;
        _resourceProfiles = resourceProfiles;
        _accountDeactivation = accountDeactivation;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetAllAsync(cancellationToken);
        return users.Select(MapUser).ToList();
    }

    public async Task DeactivateAsync(int id, int actingUserId, CancellationToken cancellationToken = default)
    {
        UserGuard.EnsureNotSelfDeactivation(actingUserId, id);

        var user = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(id, cancellationToken),
            ErrorMessages.UserNotFound);

        var oldUserSnapshot = AuditSnapshotBuilder.UserSnapshot(user);
        var linkedProfile = await _resourceProfiles.GetByUserIdAsync(id, cancellationToken);
        object? oldProfileSnapshot = linkedProfile is null
            ? null
            : AuditSnapshotBuilder.ResourceProfileSnapshot(linkedProfile);

        await _accountDeactivation.DeactivateUserAsync(user, actingUserId, cancellationToken);

        var updatedUser = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(id, cancellationToken),
            ErrorMessages.UserNotFound);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.User,
            updatedUser.Id,
            AuditConstants.Actions.Deactivated,
            oldUserSnapshot,
            AuditSnapshotBuilder.UserSnapshot(updatedUser),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        if (linkedProfile is not null)
        {
            var updatedProfile = await _resourceProfiles.GetByIdAsync(linkedProfile.Id, cancellationToken);
            if (updatedProfile is not null)
            {
                await _auditLog.AuditAsync(
                    AuditConstants.EntityNames.ResourceProfile,
                    updatedProfile.Id,
                    AuditConstants.Actions.Deactivated,
                    oldProfileSnapshot,
                    AuditSnapshotBuilder.ResourceProfileSnapshot(updatedProfile),
                    actingUserId,
                    AuthConstants.RoleName(UserRole.Admin),
                    AuditConstants.Sources.User,
                    cancellationToken);
            }
        }

        _logger.LogInformation("User deactivated. UserId={UserId}, Username={Username}", user.Id, user.Username);
    }

    public async Task ReactivateAsync(int id, int actingUserId, CancellationToken cancellationToken = default)
    {
        var user = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(id, cancellationToken),
            ErrorMessages.UserNotFound);

        var oldUserSnapshot = AuditSnapshotBuilder.UserSnapshot(user);
        var linkedProfile = await _resourceProfiles.GetByUserIdAsync(id, cancellationToken);
        object? oldProfileSnapshot = linkedProfile is null
            ? null
            : AuditSnapshotBuilder.ResourceProfileSnapshot(linkedProfile);

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.User,
            user.Id,
            AuditConstants.Actions.Reactivated,
            oldUserSnapshot,
            AuditSnapshotBuilder.UserSnapshot(user),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        if (linkedProfile is not null)
        {
            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.ResourceProfile,
                linkedProfile.Id,
                AuditConstants.Actions.Reactivated,
                oldProfileSnapshot,
                AuditSnapshotBuilder.ResourceProfileSnapshot(linkedProfile),
                actingUserId,
                AuthConstants.RoleName(UserRole.Admin),
                AuditConstants.Sources.User,
                cancellationToken);
        }

        _logger.LogInformation("User reactivated. UserId={UserId}, Username={Username}", user.Id, user.Username);
    }

    private static UserListItemDto MapUser(Domain.Entities.User user) =>
        new(user.Id, user.Username, user.FullName, UserRoleHelper.GetPrimaryRoleName(user), user.IsActive);
}
