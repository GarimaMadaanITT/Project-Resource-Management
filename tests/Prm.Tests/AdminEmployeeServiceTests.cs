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
