using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Admin.Users;

public class AdminUserCredentialService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AdminUserCredentialService> _logger;

    public AdminUserCredentialService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLog,
        ILogger<AdminUserCredentialService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task ResetPasswordAsync(
        int id,
        ResetPasswordRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var newPassword = StringGuard.RequireNonEmpty(request.NewTemporaryPassword, "New temporary password");
        PasswordGuard.EnsureValid(newPassword);

        var user = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(id, cancellationToken),
            ErrorMessages.UserNotFound);

        var oldSnapshot = AuditSnapshotBuilder.PasswordResetSnapshot(user.ForcePasswordChange);

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.ForcePasswordChange = DomainDefaults.ForcePasswordChangeOnReset;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.User,
            user.Id,
            AuditConstants.Actions.PasswordReset,
            oldSnapshot,
            AuditSnapshotBuilder.PasswordResetSnapshot(user.ForcePasswordChange),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        _logger.LogInformation("Password reset. UserId={UserId}, Username={Username}", user.Id, user.Username);
    }
}
