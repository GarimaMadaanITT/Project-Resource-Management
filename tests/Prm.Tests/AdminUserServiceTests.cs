using Moq;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Services.Admin;
using Prm.Application.Services.Admin.Users;
using Prm.Application.Services.Shared;
using Prm.Domain.Entities;
using Prm.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Prm.Tests;

public class AdminUserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IResourceProfileRepository> _resourceProfiles = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAuditLogService> _auditLog = new();

    public AdminUserServiceTests()
    {
        _hasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("hashed");
        _roles.Setup(repository => repository.EnsureSeededAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _roles.Setup(repository => repository.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string roleName, CancellationToken _) => new Role { Id = 1, RoleName = roleName });
    }

    [Fact]
    public async Task CreateAsync_Throws_Before_Save_When_Department_Missing_For_Manager()
    {
        var service = CreateService();
        var request = new CreateUserRequest(
            "New Manager",
            "manager@test.com",
            "new.manager",
            "Manager@99",
            "Manager",
            null,
            null);

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(request, 99));

        _users.Verify(
            repository => repository.CreateWithResourceProfileAsync(
                It.IsAny<User>(),
                It.IsAny<ResourceProfile?>(),
                It.IsAny<UserRoleAssignment>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _users.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Throws_ConflictException_For_Duplicate_Username()
    {
        _users.Setup(repository => repository.ExistsUsernameAsync("dup.user", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = CreateService();
        var request = new CreateUserRequest(
            "Dup User",
            "dup@test.com",
            "dup.user",
            "Manager@99",
            "Admin",
            null,
            null);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request, 99));
    }

    [Fact]
    public async Task DeactivateAsync_Throws_ForbiddenException_For_Self_Deactivation()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeactivateAsync(1, 1));
    }

    private AdminUserService CreateService()
    {
        var accountDeactivation = new AccountDeactivationService(
            _resourceProfiles.Object,
            _users.Object,
            _projects.Object,
            _auditLog.Object,
            NullLogger<AccountDeactivationService>.Instance);

        return new AdminUserService(
            new AdminUserProvisioningService(
                _users.Object,
                _roles.Object,
                _hasher.Object,
                _auditLog.Object,
                NullLogger<AdminUserProvisioningService>.Instance),
            new AdminUserCredentialService(
                _users.Object,
                _hasher.Object,
                _auditLog.Object,
                NullLogger<AdminUserCredentialService>.Instance),
            new AdminUserLifecycleService(
                _users.Object,
                _resourceProfiles.Object,
                accountDeactivation,
                _auditLog.Object,
                NullLogger<AdminUserLifecycleService>.Instance));
    }
}
