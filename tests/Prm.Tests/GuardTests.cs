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
        Assert.Throws<DomainException>(() => EnumGuard.Parse<EmployeeStatus>("Invalid", "Status"));
    }

    [Fact]
    public void Parse_Returns_Enum_For_Valid_Value()
    {
        var status = EnumGuard.Parse<EmployeeStatus>("Bench", "Status");
        Assert.Equal(EmployeeStatus.Bench, status);
    }
}

public class SettingsValidatorTests
{
    [Fact]
    public void EnsurePositive_Throws_For_Zero()
    {
        Assert.Throws<DomainException>(() => SettingsValidator.EnsurePositive(0, "Scheduler interval"));
    }
}
