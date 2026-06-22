using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class DomainEntityTests
{
    [Fact]
    public void User_Entity_Has_Expected_Defaults()
    {
        var user = new User
        {
            Username = "test.user",
            Email = "test@techserve.com",
            FullName = "Test User",
            PasswordHash = "hash",
            UserRoles =
            [
                new UserRoleAssignment
                {
                    IsPrimary = true,
                    Role = new Role { RoleName = "Employee" }
                }
            ]
        };

        Assert.True(user.IsActive);
        Assert.Equal("test.user", user.Username);
    }

    [Fact]
    public void SystemSetting_Has_Default_MaxWeeklyHours()
    {
        var settings = new SystemSetting();
        Assert.Equal(40, settings.MaxWeeklyHours);
        Assert.Equal(4, settings.SchedulerIntervalHours);
    }
}
