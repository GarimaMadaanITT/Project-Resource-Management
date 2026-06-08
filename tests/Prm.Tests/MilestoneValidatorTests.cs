using Prm.Application.Validation;
using Prm.Domain.Entities;
using Prm.Domain.Exceptions;

namespace Prm.Tests;

public class MilestoneValidatorTests
{
    [Fact]
    public void ValidateDueDateWithinProject_Throws_When_Outside_Range()
    {
        var project = new Project
        {
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30)
        };

        Assert.Throws<DomainException>(() =>
            MilestoneValidator.ValidateDueDateWithinProject(new DateOnly(2026, 7, 1), project));
    }

    [Fact]
    public void ValidateStoryPointBudget_Throws_When_Exceeding_Total()
    {
        Assert.Throws<DomainException>(() =>
            MilestoneValidator.ValidateStoryPointBudget(80, 30, 100));
    }

    [Fact]
    public void ValidateStoryPointBudget_Allows_Exact_Budget()
    {
        MilestoneValidator.ValidateStoryPointBudget(80, 20, 100);
    }
}
