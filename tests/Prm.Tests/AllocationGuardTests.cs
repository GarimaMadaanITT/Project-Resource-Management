using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class AllocationGuardTests
{
    [Fact]
    public void EnsureEmployeeCanReceiveAllocation_Throws_For_Inactive_Employee()
    {
        var resourceProfile = TestDataHelpers.CreateResourceProfile(isActive: false);

        var ex = Assert.Throws<DomainException>(() => AllocationGuard.EnsureEmployeeCanReceiveAllocation(resourceProfile));
        Assert.Contains("Inactive employees cannot receive project allocations", ex.Message);
    }
}
