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
    private readonly Mock<IResourceProfileRepository> _resourceProfiles = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<ISkillRepository> _skills = new();
    private readonly Mock<IAuditLogService> _auditLog = new();

    [Fact]
    public async Task AssignManagerAsync_Throws_When_Employee_Inactive()
    {
        var resourceProfile = TestDataHelpers.CreateResourceProfile(1, isActive: false, fullName: "Inactive");

        _resourceProfiles.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(resourceProfile);

        var service = CreateService();
        await Assert.ThrowsAsync<DomainException>(() =>
            service.AssignManagerAsync(1, new AssignManagerRequest(2), 99));
    }

    [Fact]
    public async Task DeactivateAsync_Blocks_Manager_With_Active_Team()
    {
        var managerUser = TestDataHelpers.CreateUser(UserRole.Manager, fullName: "Manager");
        managerUser.Id = 2;
        var managerResourceProfile = new ResourceProfile { Id = 10, UserId = 2, User = managerUser };

        _resourceProfiles.Setup(repository => repository.GetByIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(managerResourceProfile);
        _users.Setup(repository => repository.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(managerUser);
        _resourceProfiles.Setup(repository => repository.HasActiveTeamMembersAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _projects.Setup(repository => repository.HasActiveProjectsForManagerAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = CreateService();
        await Assert.ThrowsAsync<DomainException>(() => service.DeactivateAsync(10, 99));
    }

    [Fact]
    public async Task UpdateAsync_Accepts_Custom_Department_Label()
    {
        var user = TestDataHelpers.CreateUser(UserRole.Employee, fullName: "Anil Mehta");
        var resourceProfile = TestDataHelpers.CreateResourceProfile(1, fullName: "Anil Mehta");
        resourceProfile.User = user;

        _resourceProfiles.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(resourceProfile);
        _users.Setup(repository => repository.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var command = new AdminEmployeeCommandService(
            _resourceProfiles.Object,
            _users.Object,
            _auditLog.Object,
            NullLogger<AdminEmployeeCommandService>.Instance);

        await command.UpdateAsync(1, new UpdateEmployeeRequest("Cloud Platform", "Senior Engineer"), 99);

        Assert.Equal("Cloud Platform", resourceProfile.User.Department);
        Assert.Equal("Senior Engineer", resourceProfile.User.Designation);
    }

    [Fact]
    public async Task UpdateAsync_Throws_When_Department_Is_Numeric_Only()
    {
        var resourceProfile = TestDataHelpers.CreateResourceProfile(1, fullName: "Anil Mehta");
        _resourceProfiles.Setup(repository => repository.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(resourceProfile);

        var command = new AdminEmployeeCommandService(
            _resourceProfiles.Object,
            _users.Object,
            _auditLog.Object,
            NullLogger<AdminEmployeeCommandService>.Instance);

        await Assert.ThrowsAsync<DomainException>(() =>
            command.UpdateAsync(1, new UpdateEmployeeRequest("123", null), 99));
    }

    private AdminEmployeeService CreateService()
    {
        var accountDeactivation = new AccountDeactivationService(
            _resourceProfiles.Object,
            _users.Object,
            _projects.Object,
            _auditLog.Object,
            NullLogger<AccountDeactivationService>.Instance);

        return new AdminEmployeeService(
            _resourceProfiles.Object,
            _users.Object,
            accountDeactivation,
            new AdminEmployeeQueryService(_resourceProfiles.Object),
            new AdminEmployeeCommandService(
                _resourceProfiles.Object,
                _users.Object,
                _auditLog.Object,
                NullLogger<AdminEmployeeCommandService>.Instance),
            new AdminEmployeeSkillService(_resourceProfiles.Object, _skills.Object, _auditLog.Object));
    }
}
