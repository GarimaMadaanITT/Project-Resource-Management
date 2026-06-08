using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class EmployeeGuardTests
{
    [Fact]
    public void EnsureActive_Throws_For_Inactive_Employee()
    {
        var employee = new Employee { IsActive = false };

        var ex = Assert.Throws<DomainException>(() => EmployeeGuard.EnsureActive(employee));
        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureActiveManager_Throws_For_Inactive_Manager_User()
    {
        var managerUser = new User { Role = UserRole.Manager, IsActive = false };
        var managerEmployee = new Employee { IsActive = true };

        var ex = Assert.Throws<DomainException>(() => EmployeeGuard.EnsureActiveManager(managerUser, managerEmployee));
        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureActiveManager_Throws_When_Manager_Has_No_Employee_Profile()
    {
        var managerUser = new User { Role = UserRole.Manager, IsActive = true };

        Assert.Throws<DomainException>(() => EmployeeGuard.EnsureActiveManager(managerUser, null));
    }
}
