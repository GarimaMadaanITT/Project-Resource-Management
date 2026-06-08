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
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IProjectRepository> _projects = new();

    [Fact]
    public async Task DeactivateEmployeeAsync_Blocks_Last_Active_Admin()
    {
        var employee = new Employee { Id = 1, UserId = 1, IsActive = true };
        var user = new User { Id = 1, Role = UserRole.Admin, IsActive = true, FullName = "Admin" };

        _users.Setup(repository => repository.CountActiveAdminsAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = CreateService();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.DeactivateEmployeeAsync(employee, user, 2));
    }

    private AccountDeactivationService CreateService() =>
        new(
            _employees.Object,
            _users.Object,
            _projects.Object,
            NullLogger<AccountDeactivationService>.Instance);
}
