using Prm.Application.Validation;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class OrgLabelValidatorTests
{
    [Theory]
    [InlineData("Backend")]
    [InlineData("Cloud Platform")]
    [InlineData("QA-Automation")]
    public void ValidateRequired_Accepts_Valid_Labels(string value)
    {
        Assert.Equal(value, OrgLabelValidator.ValidateRequired(value, "Department"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("@#$")]
    public void ValidateRequired_Rejects_Invalid_Labels(string value)
    {
        Assert.Throws<DomainException>(() => OrgLabelValidator.ValidateRequired(value, "Department"));
    }

    [Fact]
    public void ValidateOptional_Returns_Null_For_Blank()
    {
        Assert.Null(OrgLabelValidator.ValidateOptional(null, "Designation"));
        Assert.Null(OrgLabelValidator.ValidateOptional("   ", "Designation"));
    }
}

public class DateGuardTests
{
    [Fact]
    public void EnsureNotInPast_Throws_For_Yesterday()
    {
        var yesterday = ActiveDateHelper.TodayUtc.AddDays(-1);
        Assert.Throws<DomainException>(() => DateGuard.EnsureNotInPast(yesterday, "Start date"));
    }

    [Fact]
    public void EnsureNotInPast_Allows_Today()
    {
        DateGuard.EnsureNotInPast(ActiveDateHelper.TodayUtc, "Start date");
    }
}

public class ProjectValidatorDateTests
{
    [Fact]
    public void ValidateDates_Throws_When_Start_Date_In_Past()
    {
        var past = ActiveDateHelper.TodayUtc.AddDays(-10);
        var future = ActiveDateHelper.TodayUtc.AddDays(30);

        Assert.Throws<DomainException>(() => ProjectValidator.ValidateDates(past, future));
    }
}
