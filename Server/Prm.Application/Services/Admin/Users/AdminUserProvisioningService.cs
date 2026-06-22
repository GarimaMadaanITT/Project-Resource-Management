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
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AdminUserProvisioningService> _logger;

    public AdminUserProvisioningService(
        IUserRepository users,
        IRoleRepository roles,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLog,
        ILogger<AdminUserProvisioningService> logger)
    {
        _users = users;
        _roles = roles;
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

        if (role == UserRole.Manager)
        {
            OrgLabelValidator.ValidateRequired(request.Department, "Department");
        }

        var department = OrgLabelValidator.ValidateOptional(request.Department, "Department");
        var designation = OrgLabelValidator.ValidateOptional(request.Designation, "Designation");

        UserAvailabilityGuard.EnsureUsernameAvailable(
            await _users.ExistsUsernameAsync(username, cancellationToken: cancellationToken));
        UserAvailabilityGuard.EnsureEmailAvailable(
            await _users.ExistsEmailAsync(email, cancellationToken: cancellationToken));

        await _roles.EnsureSeededAsync(cancellationToken);
        var roleEntity = EntityGuard.EnsureFound(
            await _roles.GetByNameAsync(AuthConstants.RoleName(role), cancellationToken),
            ErrorMessages.UserRoleNotFound);

        var user = new User
        {
            FullName = fullName,
            Email = email,
            Username = username,
            PasswordHash = _passwordHasher.Hash(temporaryPassword),
            Department = department,
            Designation = designation,
            IsActive = true,
            IsTemporaryPassword = DomainDefaults.IsTemporaryPasswordOnCreate
        };

        ResourceProfile? resourceProfile = null;
        if (role == UserRole.Employee)
        {
            resourceProfile = new ResourceProfile
            {
                ResourceStatus = DomainDefaults.NewResourceStatus
            };
        }

        var roleAssignment = new UserRoleAssignment
        {
            RoleId = roleEntity.Id,
            IsPrimary = true,
            AssignedByUserId = actingUserId
        };

        user = await _users.CreateWithResourceProfileAsync(user, resourceProfile, roleAssignment, cancellationToken);

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

        if (resourceProfile is not null)
        {
            await _auditLog.AuditAsync(
                AuditConstants.EntityNames.ResourceProfile,
                resourceProfile.Id,
                AuditConstants.Actions.Created,
                null,
                AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile),
                actingUserId,
                adminRole,
                AuditConstants.Sources.User,
                cancellationToken);
        }

        _logger.LogInformation(
            "User created. UserId={UserId}, Username={Username}, Role={Role}",
            user.Id,
            user.Username,
            UserRoleHelper.GetPrimaryRoleName(user));

        return MapUser(user);
    }

    private static UserListItemDto MapUser(User user) =>
        new(user.Id, user.Username, user.FullName, UserRoleHelper.GetPrimaryRoleName(user), user.IsActive);
}
