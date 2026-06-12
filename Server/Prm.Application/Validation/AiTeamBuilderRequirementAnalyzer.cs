using System.Text.RegularExpressions;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;

namespace Prm.Application.Validation;

public static partial class AiTeamBuilderRequirementAnalyzer
{
    public static IReadOnlyList<TeamBuilderRoleResultDto> ParseRequirementToRoles(string requirement)
    {
        var segments = SplitRoleSegments(requirement);
        return segments
            .Select(segment => new TeamBuilderRoleResultDto(
                AiTeamBuilderSkillRanker.ExtractRoleTitleFromSegment(segment),
                AiTeamBuilderSkillRanker.InferRequiredSkills(segment),
                TeamBuilderConstants.StatusGap,
                null,
                null,
                null,
                new TeamBuilderGapDto(
                    TeamBuilderConstants.GapReasonNoSkill,
                    "Pending match against benched employees.",
                    null,
                    null),
                []))
            .ToList();
    }

    public static IReadOnlyList<string> SplitRoleSegments(string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement))
        {
            return [];
        }

        var segments = RoleSegmentPattern()
            .Split(requirement)
            .Select(segment => segment.Trim().TrimEnd('.'))
            .Where(segment => segment.Length > 0)
            .ToList();

        return segments.Count > 0 ? segments : [requirement.Trim()];
    }

    [GeneratedRegex(@"\band a\b|\band an\b", RegexOptions.IgnoreCase)]
    private static partial Regex RoleSegmentPattern();
}
