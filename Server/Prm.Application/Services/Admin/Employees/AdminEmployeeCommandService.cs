using Microsoft.Extensions.Logging;

using Prm.Application.Common;

using Prm.Application.DTOs.Admin;

using Prm.Application.Interfaces;

using Prm.Application.Validation;

using Prm.Domain.Enums;

using Prm.Domain.Exceptions;



namespace Prm.Application.Services.Admin.Employees;



public class AdminEmployeeCommandService

{

    private readonly IResourceProfileRepository _resourceProfiles;

    private readonly IUserRepository _users;

    private readonly IAuditLogService _auditLog;

    private readonly ILogger<AdminEmployeeCommandService> _logger;



    public AdminEmployeeCommandService(

        IResourceProfileRepository resourceProfiles,

        IUserRepository users,

        IAuditLogService auditLog,

        ILogger<AdminEmployeeCommandService> logger)

    {

        _resourceProfiles = resourceProfiles;

        _users = users;

        _auditLog = auditLog;

        _logger = logger;

    }



    public async Task UpdateAsync(

        int id,

        UpdateEmployeeRequest request,

        int actingUserId,

        CancellationToken cancellationToken = default)

    {

        var department = OrgLabelValidator.ValidateRequired(request.Department, "Department");

        var resourceProfile = EntityGuard.EnsureFound(

            await _resourceProfiles.GetByIdAsync(id, cancellationToken),

            ErrorMessages.EmployeeNotFound);



        EmployeeGuard.EnsureActive(resourceProfile);



        var oldSnapshot = AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile);



        resourceProfile.User.Department = department;

        if (!string.IsNullOrWhiteSpace(request.Designation))
        {
            resourceProfile.User.Designation = OrgLabelValidator.ValidateRequired(
                request.Designation,
                "Designation");
        }

        resourceProfile.User.UpdatedAt = DateTime.UtcNow;

        await _users.UpdateAsync(resourceProfile.User, cancellationToken);



        await _auditLog.AuditAsync(

            AuditConstants.EntityNames.ResourceProfile,

            resourceProfile.Id,

            AuditConstants.Actions.Updated,

            oldSnapshot,

            AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile),

            actingUserId,

            AuthConstants.RoleName(UserRole.Admin),

            AuditConstants.Sources.User,

            cancellationToken);

    }



    public async Task AssignManagerAsync(

        int id,

        AssignManagerRequest request,

        int actingUserId,

        CancellationToken cancellationToken = default)

    {

        var resourceProfile = EntityGuard.EnsureFound(

            await _resourceProfiles.GetByIdAsync(id, cancellationToken),

            ErrorMessages.EmployeeNotFound);



        EmployeeGuard.EnsureActive(resourceProfile);



        var employeeUser = EntityGuard.EnsureFound(

            await _users.GetByIdAsync(resourceProfile.UserId, cancellationToken),

            ErrorMessages.LinkedUserNotFound);



        if (UserRoleHelper.GetPrimaryRole(employeeUser) != UserRole.Employee)

        {

            throw new DomainException("Only employees can have a reporting manager.");

        }



        if (resourceProfile.UserId == request.ManagerUserId)

        {

            throw new DomainException("An employee cannot be assigned as their own manager.");

        }



        var managerUser = EntityGuard.EnsureFound(

            await _users.GetByIdAsync(request.ManagerUserId, cancellationToken),

            ErrorMessages.ManagerUserNotFound);



        EmployeeGuard.EnsureActiveManager(managerUser);



        var oldSnapshot = AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile);



        resourceProfile.ManagerUserId = request.ManagerUserId;

        resourceProfile.UpdatedAt = DateTime.UtcNow;

        await _resourceProfiles.UpdateAsync(resourceProfile, cancellationToken);



        await _auditLog.AuditAsync(

            AuditConstants.EntityNames.ResourceProfile,

            resourceProfile.Id,

            AuditConstants.Actions.ManagerAssigned,

            oldSnapshot,

            AuditSnapshotBuilder.ResourceProfileSnapshot(resourceProfile),

            actingUserId,

            AuthConstants.RoleName(UserRole.Admin),

            AuditConstants.Sources.User,

            cancellationToken);



        _logger.LogInformation(

            "Manager assigned. ResourceProfileId={ResourceProfileId}, ManagerUserId={ManagerUserId}",

            resourceProfile.Id,

            request.ManagerUserId);

    }

}


