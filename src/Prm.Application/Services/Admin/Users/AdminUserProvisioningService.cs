using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Admin.Users;

public class AdminUserProvisioningService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AdminUserProvisioningService> _logger;

    public AdminUserProvisioningService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLog,
        ILogger<AdminUserProvisioningService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<UserListItemDto> CreateAsync(
        CreateUserRequest request,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        var fullName = StringGuard.RequireNonEmpty(request.FullName, "Full name");
        var email = EmailValidator.ValidateAndNormalize(request.Email);
        var username = StringGuard.RequireNonEmpty(request.Username, "Username");
        var temporaryPassword = StringGuard.RequireNonEmpty(request.TemporaryPassword, "Temporary password");
        var roleValue = StringGuard.RequireNonEmpty(request.Role, "Role");

        PasswordGuard.EnsureValid(temporaryPassword);

        var role = AuthConstants.ParseRole(roleValue);

        string? department = null;
        if (role is UserRole.Manager or UserRole.Employee)
        {
            department = StringGuard.RequireNonEmpty(request.Department, "Department");
        }

        UserAvailabilityGuard.EnsureUsernameAvailable(
            await _users.ExistsUsernameAsync(username, cancellationToken: cancellationToken));
        UserAvailabilityGuard.EnsureEmailAvailable(
            await _users.ExistsEmailAsync(email, cancellationToken: cancellationToken));

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Username = username,
            PasswordHash = _passwordHasher.Hash(temporaryPassword),
            Role = role,
            IsActive = true,
            ForcePasswordChange = DomainDefaults.ForcePasswordChangeOnCreate
        };

        Employee? employee = null;
        if (role is UserRole.Manager or UserRole.Employee)
        {
            employee = new Employee
            {
                Department = department!,
                Status = DomainDefaults.NewEmployeeStatus,
                IsActive = true
            };
        }

        user = await _users.CreateWithEmployeeAsync(user, employee, cancellationToken);

        var adminRole = AuthConstants.RoleName(UserRole.Admin);
        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.User,
            user.Id,
            AuditConstants.Actions.Created,
            null,
            AuditSnapshotBuilder.UserSnapshot(user),
            actingUserId,
            adminRole,
            AuditConstants.Sources.User,
            cancellationToken);

        if (employee is not null)
        {
            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.Employee,
                employee.Id,
                AuditConstants.Actions.Created,
                null,
                AuditSnapshotBuilder.EmployeeSnapshot(employee),
                actingUserId,
                adminRole,
                AuditConstants.Sources.User,
                cancellationToken);
        }

        _logger.LogInformation(
            "User created. UserId={UserId}, Username={Username}, Role={Role}",
            user.Id,
            user.Username,
            AuthConstants.RoleName(user.Role));

        return MapUser(user);
    }

    private static UserListItemDto MapUser(User user) =>
        new(user.Id, user.Username, user.FullName, AuthConstants.RoleName(user.Role), user.IsActive);
}
