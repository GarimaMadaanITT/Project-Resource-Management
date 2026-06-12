using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Common;

public static class UserRoleHelper
{
    public static UserRole GetPrimaryRole(User user)
    {
        var assignment = user.UserRoles.FirstOrDefault(r => r.IsPrimary)
            ?? user.UserRoles.FirstOrDefault();

        if (assignment?.Role?.RoleName is null)
        {
            throw new DomainException(ErrorMessages.UserRoleNotFound);
        }

        return AuthConstants.ParseRole(assignment.Role.RoleName);
    }

    public static string GetPrimaryRoleName(User user) =>
        AuthConstants.RoleName(GetPrimaryRole(user));
}
