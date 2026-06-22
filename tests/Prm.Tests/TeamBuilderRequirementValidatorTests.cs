using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class TeamBuilderRequirementValidatorTests
{
    [Fact]
    public void Validate_Accepts_Valid_Requirement()
    {
        var result = TeamBuilderRequirementValidator.Validate("Need a Java developer and a QA tester.");
        Assert.Equal("Need a Java developer and a QA tester.", result);
    }

    [Fact]
    public void Validate_Throws_For_Empty_Requirement()
    {
        Assert.Throws<DomainException>(() => TeamBuilderRequirementValidator.Validate("   "));
    }

    [Fact]
    public void Validate_Throws_When_Requirement_Too_Long()
    {
        var longText = new string('a', 1001);
        Assert.Throws<DomainException>(() => TeamBuilderRequirementValidator.Validate(longText));
    }
}
