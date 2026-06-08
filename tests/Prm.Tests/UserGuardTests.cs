using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class UserGuardTests
{
    [Fact]
    public void EnsureNotSelfDeactivation_Throws_When_Same_User()
    {
        Assert.Throws<ForbiddenException>(() => UserGuard.EnsureNotSelfDeactivation(1, 1));
    }

    [Fact]
    public void EnsureNotLastActiveAdmin_Throws_When_Count_Is_One()
    {
        Assert.Throws<ForbiddenException>(() => UserGuard.EnsureNotLastActiveAdmin(1));
    }
}
