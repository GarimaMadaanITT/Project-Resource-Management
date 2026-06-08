using Moq;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Services.Admin;
using Prm.Application.Services.Admin.Employees;
using Prm.Application.Services.Shared;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Prm.Tests;

public class AdminEmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<ISkillRepository> _skills = new();

    [Fact]
    public async Task AssignManagerAsync_Throws_When_Employee_Inactive()
    {
        var employee = new Employee
        {
            Id = 1,
            IsActive = false,
            User = new User { FullName = "Inactive" }
        };

        _employees.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(employee);

        var service = CreateService();
        await Assert.ThrowsAsync<DomainException>(() =>
            service.AssignManagerAsync(1, new AssignManagerRequest(2)));
    }

    [Fact]
    public async Task DeactivateAsync_Blocks_Manager_With_Active_Team()
    {
        var managerEmployee = new Employee { Id = 10, UserId = 2, IsActive = true };
        var managerUser = new User { Id = 2, Role = UserRole.Manager, IsActive = true, FullName = "Manager" };

        _employees.Setup(repository => repository.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(managerEmployee);
        _users.Setup(repository => repository.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(managerUser);
        _employees.Setup(repository => repository.HasActiveTeamMembersAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projects.Setup(repository => repository.HasActiveProjectsForManagerAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateService();
        await Assert.ThrowsAsync<DomainException>(() => service.DeactivateAsync(10, 99));
    }

    private AdminEmployeeService CreateService()
    {
        var accountDeactivation = new AccountDeactivationService(
            _employees.Object,
            _users.Object,
            _projects.Object,
            NullLogger<AccountDeactivationService>.Instance);

        return new AdminEmployeeService(
            _employees.Object,
            _users.Object,
            accountDeactivation,
            new AdminEmployeeQueryService(_employees.Object),
            new AdminEmployeeCommandService(
                _employees.Object,
                _users.Object,
                NullLogger<AdminEmployeeCommandService>.Instance),
            new AdminEmployeeSkillService(_employees.Object, _skills.Object));
    }
}
