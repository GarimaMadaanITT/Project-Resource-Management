using Prm.Application.Validation;

namespace Prm.Application.Services.Notifications;

public static class AtRiskSkillRequirementDeriver
{
    public static string Derive(IReadOnlyList<ProjectRiskFlagCalculator.RiskFlag> flags)
    {
        var riskFlags = flags.Where(flag => flag.IsRisk).ToList();
        if (riskFlags.Count == 0)
        {
            return "general project support 20 hrs/week";
        }

        foreach (var flag in riskFlags)
        {
            if (flag.Code == "OVER_ALLOCATED")
            {
                return "additional developer capacity 20 hrs/week";
            }

            if (flag.Code == "MILESTONE_OVERDUE")
            {
                return "delivery support milestone recovery 30 hrs/week";
            }

            if (flag.Code == "LOW_HOURS")
            {
                return "backfill resource timesheet compliance 20 hrs/week";
            }
        }

        return riskFlags[0].Message;
    }
}
