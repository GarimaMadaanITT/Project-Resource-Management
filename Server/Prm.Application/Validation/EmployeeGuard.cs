using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class EmployeeGuard
{
    public static void EnsureActive(ResourceProfile resourceProfile)
    {
        if (!resourceProfile.User.IsActive)
        {
            throw new DomainException("Employee is inactive. This action is not allowed.");
        }
    }

    public static void EnsureNotAlreadyInactive(ResourceProfile resourceProfile)
    {
        if (!resourceProfile.User.IsActive)
        {
            throw new DomainException(ErrorMessages.EmployeeAlreadyInactive);
        }
    }

    public static void EnsureActiveManager(User managerUser)
    {
        if (UserRoleHelper.GetPrimaryRole(managerUser) != UserRole.Manager)
        {
            throw new DomainException("Selected user is not a Manager.");
        }

        if (!managerUser.IsActive)
        {
            throw new DomainException("Manager account is inactive.");
        }
    }
}
