using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class StringGuardTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RequireNonEmpty_Throws_For_Invalid_Values(string? value)
    {
        var ex = Assert.Throws<DomainException>(() => StringGuard.RequireNonEmpty(value, "Username"));
        Assert.Contains("Username is required", ex.Message);
    }

    [Fact]
    public void RequireNonEmpty_Trims_Valid_Value()
    {
        var result = StringGuard.RequireNonEmpty("  admin  ", "Username");
        Assert.Equal("admin", result);
    }
}
