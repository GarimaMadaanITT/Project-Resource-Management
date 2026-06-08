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
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly IProjectRepository _projects;
    private readonly ILogger<AccountDeactivationService> _logger;

    public AccountDeactivationService(
        IEmployeeRepository employees,
        IUserRepository users,
        IProjectRepository projects,
        ILogger<AccountDeactivationService> logger)
    {
        _employees = employees;
        _users = users;
        _projects = projects;
        _logger = logger;
    }

    public async Task<DeactivateEmployeeResponse> DeactivateEmployeeAsync(
        Employee employee,
        User user,
        int actingUserId,
        CancellationToken cancellationToken = default)
    {
        UserGuard.EnsureNotSelfDeactivation(actingUserId, employee.UserId);
        EmployeeGuard.EnsureNotAlreadyInactive(employee);
        await ValidateCanDeactivateUserAsync(user, employee.Id, cancellationToken);

        var today = ActiveDateHelper.TodayUtc;
        var activeAllocations = await _employees.GetActiveAllocationsAsync(employee.Id, cancellationToken);
        await _employees.EndActiveAllocationsAsync(employee.Id, today, cancellationToken);

        await ApplyDeactivationAsync(user, employee, cancellationToken);

        _logger.LogInformation(
            "Employee deactivated. EmployeeId={EmployeeId}, UserId={UserId}, AllocationsEnded={AllocationCount}",
            employee.Id,
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

        var managerEmployee = await _employees.GetByUserIdAsync(user.Id, cancellationToken);
        var managerEmployeeId = managerEmployee?.Id ?? 0;
        await ValidateCanDeactivateUserAsync(user, managerEmployeeId, cancellationToken);

        var linkedEmployee = await _employees.GetByUserIdAsync(user.Id, cancellationToken);
        await ApplyDeactivationAsync(user, linkedEmployee, cancellationToken);
    }

    private async Task ValidateCanDeactivateUserAsync(User user, int managerEmployeeId, CancellationToken cancellationToken)
    {
        if (user.Role == UserRole.Admin)
        {
            var activeAdminCount = await _users.CountActiveAdminsAsync(cancellationToken: cancellationToken);
            UserGuard.EnsureNotLastActiveAdmin(activeAdminCount);
        }

        if (user.Role != UserRole.Manager)
        {
            return;
        }

        var hasTeam = managerEmployeeId > 0 &&
                      await _employees.HasActiveTeamMembersAsync(managerEmployeeId, cancellationToken);
        var hasProjects = await _projects.HasActiveProjectsForManagerAsync(user.Id, cancellationToken);
        ManagerDeactivationGuard.EnsureCanDeactivateManager(hasTeam, hasProjects);
    }

    private async Task ApplyDeactivationAsync(User user, Employee? employee, CancellationToken cancellationToken)
    {
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        if (employee is null)
        {
            return;
        }

        employee.IsActive = false;
        employee.Status = DomainDefaults.NewEmployeeStatus;
        employee.UpdatedAt = DateTime.UtcNow;
        await _employees.UpdateAsync(employee, cancellationToken);
    }
}
