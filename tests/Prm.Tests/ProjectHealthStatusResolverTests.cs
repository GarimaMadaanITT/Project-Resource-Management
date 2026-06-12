using Prm.Application.Validation;
using Prm.Domain.Enums;

namespace Prm.Tests;

public class ProjectHealthStatusResolverTests
{
    [Fact]
    public void Resolve_Returns_AtRisk_For_Milestone_Overdue()
    {
        var flags = new List<ProjectRiskFlagCalculator.RiskFlag>
        {
            new("MILESTONE_OVERDUE", "Milestone overdue", true)
        };

        Assert.Equal(HealthStatus.AtRisk, ProjectHealthStatusResolver.Resolve(flags));
    }

    [Fact]
    public void Resolve_Returns_Attention_For_Low_Hours()
    {
        var flags = new List<ProjectRiskFlagCalculator.RiskFlag>
        {
            new("LOW_HOURS", "Low hours logged", true)
        };

        Assert.Equal(HealthStatus.Attention, ProjectHealthStatusResolver.Resolve(flags));
    }

    [Fact]
    public void Resolve_Returns_OnTrack_When_No_Risk_Flags()
    {
        var flags = new List<ProjectRiskFlagCalculator.RiskFlag>
        {
            new("ON_TRACK", "All good", false)
        };

        Assert.Equal(HealthStatus.OnTrack, ProjectHealthStatusResolver.Resolve(flags));
    }
}
