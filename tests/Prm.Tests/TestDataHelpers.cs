using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;

namespace Prm.Tests;

internal static class TestDataHelpers
{
    public static User CreateUser(UserRole role, bool isActive = true, string fullName = "Test User")
    {
        var roleName = AuthConstants.RoleName(role);
        return new User
        {
            FullName = fullName,
            IsActive = isActive,
            UserRoles =
            [
                new UserRoleAssignment
                {
                    IsPrimary = true,
                    Role = new Role { RoleName = roleName }
                }
            ]
        };
    }

    public static ResourceProfile CreateResourceProfile(
        int id = 1,
        bool isActive = true,
        int? managerUserId = null,
        string fullName = "Test User")
    {
        var user = CreateUser(UserRole.Employee, isActive, fullName);
        user.Id = id;

        return new ResourceProfile
        {
            Id = id,
            UserId = id,
            ManagerUserId = managerUserId,
            User = user
        };
    }
}
