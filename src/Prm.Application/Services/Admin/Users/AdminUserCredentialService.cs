using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Admin.Users;

public class AdminUserCredentialService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AdminUserCredentialService> _logger;

    public AdminUserCredentialService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ILogger<AdminUserCredentialService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var newPassword = StringGuard.RequireNonEmpty(request.NewTemporaryPassword, "New temporary password");
        PasswordGuard.EnsureValid(newPassword);

        var user = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(id, cancellationToken),
            ErrorMessages.UserNotFound);

        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.ForcePasswordChange = DomainDefaults.ForcePasswordChangeOnReset;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Password reset. UserId={UserId}, Username={Username}", user.Id, user.Username);
    }
}
