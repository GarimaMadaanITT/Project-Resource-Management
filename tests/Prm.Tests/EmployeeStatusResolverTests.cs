using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class EmployeeStatusResolverTests
{
    [Theory]
    [InlineData(0, ResourceStatus.Bench)]
    [InlineData(50, ResourceStatus.Allocated)]
    [InlineData(100, ResourceStatus.Allocated)]
    public void ResolveFromUtilisation_Returns_Expected_Status(int utilisation, ResourceStatus expected)
    {
        Assert.Equal(expected, EmployeeStatusResolver.ResolveFromUtilisation(utilisation));
    }
}
