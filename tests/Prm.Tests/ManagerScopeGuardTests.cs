using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class ManagerScopeGuardTests
{
    [Fact]
    public void EnsureEmployeeOnTeam_Throws_When_Not_On_Team()
    {
        var resourceProfile = new ResourceProfile { Id = 5, ManagerUserId = 99 };

        Assert.Throws<ForbiddenException>(() =>
            ManagerScopeGuard.EnsureEmployeeOnTeam(resourceProfile, managerUserId: 1));
    }

    [Fact]
    public void EnsureProjectOwnedByManager_Throws_When_Not_Owner()
    {
        var project = new Project { Id = 1, ManagerUserId = 99 };

        Assert.Throws<ForbiddenException>(() =>
            ManagerScopeGuard.EnsureProjectOwnedByManager(project, managerUserId: 2));
    }
}
