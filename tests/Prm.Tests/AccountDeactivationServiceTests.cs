using Moq;
using Prm.Application.Interfaces;
using Prm.Application.Services.Shared;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Prm.Tests;

public class AccountDeactivationServiceTests
{
    private readonly Mock<IResourceProfileRepository> _resourceProfiles = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProjectRepository> _projects = new();
    private readonly Mock<IAuditLogService> _auditLog = new();

    [Fact]
    public async Task DeactivateEmployeeAsync_Blocks_Last_Active_Admin()
    {
        var user = TestDataHelpers.CreateUser(UserRole.Admin, fullName: "Admin");
        user.Id = 1;
        var resourceProfile = new ResourceProfile { Id = 1, UserId = 1, User = user };

        _users.Setup(repository => repository.CountActiveAdminsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.DeactivateEmployeeAsync(resourceProfile, user, 2));
    }

    private AccountDeactivationService CreateService() =>
        new(
            _resourceProfiles.Object,
            _users.Object,
            _projects.Object,
            _auditLog.Object,
            NullLogger<AccountDeactivationService>.Instance);
}
