using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Validation;

namespace Prm.Application.Services.Admin.Employees;

public class AdminEmployeeCommandService
{
    private readonly IEmployeeRepository _employees;
    private readonly IUserRepository _users;
    private readonly ILogger<AdminEmployeeCommandService> _logger;

    public AdminEmployeeCommandService(
        IEmployeeRepository employees,
        IUserRepository users,
        ILogger<AdminEmployeeCommandService> logger)
    {
        _employees = employees;
        _users = users;
        _logger = logger;
    }

    public async Task UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var department = StringGuard.RequireNonEmpty(request.Department, "Department");
        var employee = EntityGuard.EnsureFound(
            await _employees.GetByIdAsync(id, cancellationToken),
            ErrorMessages.EmployeeNotFound);

        employee.Department = department;
        employee.UpdatedAt = DateTime.UtcNow;
        await _employees.UpdateAsync(employee, cancellationToken);
    }

    public async Task AssignManagerAsync(int id, AssignManagerRequest request, CancellationToken cancellationToken = default)
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

        employee.ManagerId = managerEmployee!.Id;
        employee.UpdatedAt = DateTime.UtcNow;
        await _employees.UpdateAsync(employee, cancellationToken);

        _logger.LogInformation(
            "Manager assigned. EmployeeId={EmployeeId}, ManagerUserId={ManagerUserId}",
            employee.Id,
            request.ManagerUserId);
    }
}
