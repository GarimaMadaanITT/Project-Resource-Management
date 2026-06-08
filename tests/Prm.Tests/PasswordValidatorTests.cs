using Prm.Application.Common;

namespace Prm.Tests;

public class PasswordValidatorTests
{
    [Theory]
    [InlineData("Admin@1234")]
    [InlineData("Manager@1234")]
    [InlineData("ValidPass1")]
    public void IsValid_Accepts_Strong_Passwords(string password)
    {
        var result = PasswordValidator.IsValid(password, out var error);
        Assert.True(result);
        Assert.Empty(error);
    }

    [Theory]
    [InlineData("short1", "at least 8 characters")]
    [InlineData("alllowercase1", "uppercase")]
    [InlineData("12345678", "uppercase")]
    [InlineData("NoDigitsHere", "number")]
    public void IsValid_Rejects_Weak_Passwords(string password, string expectedFragment)
    {
        var result = PasswordValidator.IsValid(password, out var error);
        Assert.False(result);
        Assert.Contains(expectedFragment, error, StringComparison.OrdinalIgnoreCase);
    }
}
