using Prm.Domain.Entities;
using Prm.Domain.Exceptions;

namespace Prm.Application.Validation;

public static class AllocationGuard
{
    public static void EnsureEmployeeCanReceiveAllocation(ResourceProfile resourceProfile)
    {
        if (!resourceProfile.User.IsActive)
        {
            throw new DomainException("Inactive employees cannot receive project allocations.");
        }
    }
}
