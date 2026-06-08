using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class AllocationGuardTests
{
    [Fact]
    public void EnsureEmployeeCanReceiveAllocation_Throws_For_Inactive_Employee()
    {
        var employee = new Employee { IsActive = false };

        var ex = Assert.Throws<DomainException>(() => AllocationGuard.EnsureEmployeeCanReceiveAllocation(employee));
        Assert.Contains("Inactive employees cannot receive project allocations", ex.Message);
    }
}
