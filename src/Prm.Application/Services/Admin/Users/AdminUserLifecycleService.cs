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
    private readonly IEmployeeRepository _employees;
    private readonly IAccountDeactivationService _accountDeactivation;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AdminUserLifecycleService> _logger;

    public AdminUserLifecycleService(
        IUserRepository users,
        IEmployeeRepository employees,
        IAccountDeactivationService accountDeactivation,
        IAuditLogService auditLog,
        ILogger<AdminUserLifecycleService> logger)
    {
        _users = users;
        _employees = employees;
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
        var linkedEmployee = await _employees.GetByUserIdAsync(id, cancellationToken);
        object? oldEmployeeSnapshot = linkedEmployee is null
            ? null
            : AuditSnapshotBuilder.EmployeeSnapshot(linkedEmployee);

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

        if (linkedEmployee is not null)
        {
            var updatedEmployee = await _employees.GetByIdAsync(linkedEmployee.Id, cancellationToken);
            if (updatedEmployee is not null)
            {
                await _auditLog.AuditAsync(
                    AuditConstants.EntityNames.Employee,
                    updatedEmployee.Id,
                    AuditConstants.Actions.Deactivated,
                    oldEmployeeSnapshot,
                    AuditSnapshotBuilder.EmployeeSnapshot(updatedEmployee),
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
        var linkedEmployee = await _employees.GetByUserIdAsync(id, cancellationToken);
        object? oldEmployeeSnapshot = linkedEmployee is null
            ? null
            : AuditSnapshotBuilder.EmployeeSnapshot(linkedEmployee);

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        if (linkedEmployee is not null)
        {
            linkedEmployee.IsActive = true;
            linkedEmployee.UpdatedAt = DateTime.UtcNow;
            await _employees.UpdateAsync(linkedEmployee, cancellationToken);
        }

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

        if (linkedEmployee is not null)
        {
            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.Employee,
                linkedEmployee.Id,
                AuditConstants.Actions.Reactivated,
                oldEmployeeSnapshot,
                AuditSnapshotBuilder.EmployeeSnapshot(linkedEmployee),
                actingUserId,
                AuthConstants.RoleName(UserRole.Admin),
                AuditConstants.Sources.User,
                cancellationToken);
        }

        _logger.LogInformation("User reactivated. UserId={UserId}, Username={Username}", user.Id, user.Username);
    }

    private static UserListItemDto MapUser(Domain.Entities.User user) =>
        new(user.Id, user.Username, user.FullName, AuthConstants.RoleName(user.Role), user.IsActive);
}
