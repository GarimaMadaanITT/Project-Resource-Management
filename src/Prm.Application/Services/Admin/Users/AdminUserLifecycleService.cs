using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Admin.Users;

public class AdminUserLifecycleService
{
    private readonly IUserRepository _users;
    private readonly IEmployeeRepository _employees;
    private readonly IAccountDeactivationService _accountDeactivation;
    private readonly ILogger<AdminUserLifecycleService> _logger;

    public AdminUserLifecycleService(
        IUserRepository users,
        IEmployeeRepository employees,
        IAccountDeactivationService accountDeactivation,
        ILogger<AdminUserLifecycleService> logger)
    {
        _users = users;
        _employees = employees;
        _accountDeactivation = accountDeactivation;
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

        await _accountDeactivation.DeactivateUserAsync(user, actingUserId, cancellationToken);

        _logger.LogInformation("User deactivated. UserId={UserId}, Username={Username}", user.Id, user.Username);
    }

    public async Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(id, cancellationToken),
            ErrorMessages.UserNotFound);

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        var employee = await _employees.GetByUserIdAsync(id, cancellationToken);
        if (employee is not null)
        {
            employee.IsActive = true;
            employee.UpdatedAt = DateTime.UtcNow;
            await _employees.UpdateAsync(employee, cancellationToken);
        }

        _logger.LogInformation("User reactivated. UserId={UserId}, Username={Username}", user.Id, user.Username);
    }

    private static UserListItemDto MapUser(Domain.Entities.User user) =>
        new(user.Id, user.Username, user.FullName, AuthConstants.RoleName(user.Role), user.IsActive);
}
