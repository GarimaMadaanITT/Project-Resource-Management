using Prm.Application.Validation;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class EmployeeGuardTests
{
    [Fact]
    public void EnsureActive_Throws_For_Inactive_Employee()
    {
        var resourceProfile = TestDataHelpers.CreateResourceProfile(isActive: false);

        var ex = Assert.Throws<DomainException>(() => EmployeeGuard.EnsureActive(resourceProfile));
        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureActiveManager_Throws_For_Inactive_Manager_User()
    {
        var managerUser = TestDataHelpers.CreateUser(UserRole.Manager, isActive: false);

        var ex = Assert.Throws<DomainException>(() => EmployeeGuard.EnsureActiveManager(managerUser));
        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureActiveManager_Throws_When_User_Is_Not_Manager()
    {
        var employeeUser = TestDataHelpers.CreateUser(UserRole.Employee);

        Assert.Throws<DomainException>(() => EmployeeGuard.EnsureActiveManager(employeeUser));
    }
}
