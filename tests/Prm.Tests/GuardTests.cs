using Prm.Application.Common;
using Prm.Application.Validation;
using Prm.Domain.Enums;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class PasswordGuardTests
{
    [Fact]
    public void EnsureValid_Throws_For_Short_Password()
    {
        var exception = Assert.Throws<DomainException>(() => PasswordGuard.EnsureValid("Ab1"));
        Assert.Contains("8", exception.Message);
    }
}

public class EntityGuardTests
{
    [Fact]
    public void EnsureFound_Throws_KeyNotFoundException_When_Null()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            EntityGuard.EnsureFound<string>(null, ErrorMessages.UserNotFound));
    }
}

public class EnumGuardTests
{
    [Fact]
    public void Parse_Throws_For_Invalid_Value()
    {
        Assert.Throws<DomainException>(() => EnumGuard.Parse<ResourceStatus>("Invalid", "Status"));
    }

    [Fact]
    public void Parse_Returns_Enum_For_Valid_Value()
    {
        var status = EnumGuard.Parse<ResourceStatus>("Bench", "Status");
        Assert.Equal(ResourceStatus.Bench, status);
    }
}

public class SettingsValidatorTests
{
    [Fact]
    public void EnsurePositive_Throws_For_Zero()
    {
        Assert.Throws<DomainException>(() => SettingsValidator.EnsurePositive(0, "Scheduler interval"));
    }

    [Fact]
    public void ParseProvider_Accepts_Ollama()
    {
        var provider = SettingsValidator.ParseProvider("Ollama");
        Assert.Equal(LlmProviderType.Ollama, provider);
    }

    [Fact]
    public void NormalizeApiKey_Trims_Value()
    {
        var key = SettingsValidator.NormalizeApiKey("  test-key  ");
        Assert.Equal("test-key", key);
    }

    [Fact]
    public void NormalizeApiKey_Throws_For_Empty()
    {
        Assert.Throws<DomainException>(() => SettingsValidator.NormalizeApiKey("   "));
    }
}
