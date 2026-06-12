using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Application.Services.Admin.Employees;

public class AdminEmployeeCommandService
{
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<AdminEmployeeCommandService> _logger;

    public AdminEmployeeCommandService(
        IEmployeeRepository employees,
        IUserRepository users,
        IAuditLogService auditLog,
        ILogger<AdminEmployeeCommandService> logger)
    {
        _employees = employees;
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
        var department = StringGuard.RequireNonEmpty(request.Department, "Department");
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        var oldSnapshot = AuditSnapshotBuilder.EmployeeSnapshot(employee);

        employee.Department = department;
        employee.UpdatedAt = DateTime.UtcNow;
        await _employees.UpdateAsync(employee, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Employee,
            employee.Id,
            AuditConstants.Actions.Updated,
            oldSnapshot,
            AuditSnapshotBuilder.EmployeeSnapshot(employee),
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
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        EmployeeGuard.EnsureActive(employee);

        var managerUser = EntityGuard.EnsureFound(
            await _users.GetByIdAsync(request.ManagerUserId, cancellationToken),
            ErrorMessages.ManagerUserNotFound);

        var managerEmployee = await _employees.GetByUserIdAsync(request.ManagerUserId, cancellationToken);
        EmployeeGuard.EnsureActiveManager(managerUser, managerEmployee);

        var oldSnapshot = AuditSnapshotBuilder.EmployeeSnapshot(employee);

        employee.ManagerId = managerEmployee!.Id;
        employee.UpdatedAt = DateTime.UtcNow;
        await _employees.UpdateAsync(employee, cancellationToken);

        await _auditLog.AuditAsync(
            AuditConstants.EntityNames.Employee,
            employee.Id,
            AuditConstants.Actions.ManagerAssigned,
            oldSnapshot,
            AuditSnapshotBuilder.EmployeeSnapshot(employee),
            actingUserId,
            AuthConstants.RoleName(UserRole.Admin),
            AuditConstants.Sources.User,
            cancellationToken);

        _logger.LogInformation(
            "Manager assigned. EmployeeId={EmployeeId}, ManagerUserId={ManagerUserId}",
            employee.Id,
            request.ManagerUserId);
    }
}
