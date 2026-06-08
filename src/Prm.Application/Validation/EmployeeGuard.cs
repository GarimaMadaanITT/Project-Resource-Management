using Prm.Application.Common;
using Prm.Domain.Entities;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class EmployeeGuard
{
    public static void EnsureActive(Employee employee)
    {
        if (!employee.IsActive)
        {
            throw new DomainException("Employee is inactive. This action is not allowed.");
        }
    }

    public static void EnsureNotAlreadyInactive(Employee employee)
    {
        if (!employee.IsActive)
        {
            throw new DomainException(ErrorMessages.EmployeeAlreadyInactive);
        }
    }

    public static void EnsureActiveManager(User managerUser, Employee? managerEmployee)
    {
        if (managerUser.Role != UserRole.Manager)
        {
            throw new DomainException("Selected user is not a Manager.");
        }

        if (!managerUser.IsActive)
        {
            throw new DomainException("Manager account is inactive.");
        }

        if (managerEmployee is null)
        {
            throw new DomainException("Manager must have an employee profile.");
        }

        if (!managerEmployee.IsActive)
        {
            throw new DomainException("Manager employee profile is inactive.");
        }
    }
}
