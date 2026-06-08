using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class EmployeeStatusResolverTests
{
    [Theory]
    [InlineData(0, EmployeeStatus.Bench)]
    [InlineData(50, EmployeeStatus.Allocated)]
    [InlineData(100, EmployeeStatus.Allocated)]
    public void ResolveFromUtilisation_Returns_Expected_Status(int utilisation, EmployeeStatus expected)
    {
        Assert.Equal(expected, EmployeeStatusResolver.ResolveFromUtilisation(utilisation));
    }
}
