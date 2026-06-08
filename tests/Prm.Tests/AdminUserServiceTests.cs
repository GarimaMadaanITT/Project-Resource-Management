using Moq;
using Prm.Application.DTOs.Admin;
using Prm.Application.Interfaces;
using Prm.Application.Services.Admin;
using Prm.Application.Services.Admin.Users;
using Prm.Application.Services.Shared;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Prm.Tests;

public class AdminUserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IPasswordHasher> _hasher = new();

    public AdminUserServiceTests()
    {
        _hasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("hashed");
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
            null);

        await Assert.ThrowsAsync<DomainException>(() => service.CreateAsync(request));

        _users.Verify(
            repository => repository.CreateWithEmployeeAsync(It.IsAny<User>(), It.IsAny<Employee?>(), It.IsAny<CancellationToken>()),
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
            null);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(request));
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
            _employees.Object,
            _users.Object,
            _projects.Object,
            NullLogger<AccountDeactivationService>.Instance);

        return new AdminUserService(
            new AdminUserProvisioningService(
                _users.Object,
                _hasher.Object,
                NullLogger<AdminUserProvisioningService>.Instance),
            new AdminUserCredentialService(
                _users.Object,
                _hasher.Object,
                NullLogger<AdminUserCredentialService>.Instance),
            new AdminUserLifecycleService(
                _users.Object,
                _employees.Object,
                accountDeactivation,
                NullLogger<AdminUserLifecycleService>.Instance));
    }
}
