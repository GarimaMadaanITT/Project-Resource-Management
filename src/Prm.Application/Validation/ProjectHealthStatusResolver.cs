using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class ProjectHealthStatusResolver
{
    public static HealthStatus Resolve(IReadOnlyList<ProjectRiskFlagCalculator.RiskFlag> flags)
    {
        if (flags.Any(flag => flag.IsRisk && flag.Code is "MILESTONE_OVERDUE" or "OVER_ALLOCATED"))
        {
            return HealthStatus.AtRisk;
        }

        if (flags.Any(flag => flag.IsRisk && flag.Code == "LOW_HOURS"))
        {
            return HealthStatus.Attention;
        }

        return HealthStatus.OnTrack;
    }
}
