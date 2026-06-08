using Prm.Domain.Entities;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class AllocationGuard
{
    public static void EnsureEmployeeCanReceiveAllocation(Employee employee)
    {
        if (!employee.IsActive)
        {
            throw new DomainException("Inactive employees cannot receive project allocations.");
        }
    }
}
