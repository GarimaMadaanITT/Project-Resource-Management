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

        var bulletSegments = ExtractBulletRoleLines(requirement);
        if (bulletSegments.Count >= 2)
        {
            return bulletSegments;
        }

        var segments = RoleSegmentPattern()
            .Split(requirement)
            .Select(segment => segment.Trim().TrimEnd('.'))
            .Where(segment => segment.Length > 0 && !IsMetadataLine(segment))
            .ToList();

        if (segments.Count >= 2)
        {
            return segments;
        }

        return segments.Count > 0 ? segments : [requirement.Trim()];
    }

    private static List<string> ExtractBulletRoleLines(string requirement)
    {
        var results = new List<string>();

        foreach (var rawLine in requirement.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var line = rawLine.Trim();
            if (line.StartsWith('•') || line.StartsWith('-') || line.StartsWith('*'))
            {
                line = line.TrimStart('•', '-', '*', ' ').Trim();
            }
            else if (NumberedBulletPattern().IsMatch(line))
            {
                line = NumberedBulletPattern().Replace(line, string.Empty).Trim();
            }
            else
            {
                continue;
            }

            if (IsMetadataLine(line) || line.Length <= 2)
            {
                continue;
            }

            results.Add(line);
        }

        return results;
    }

    private static bool IsMetadataLine(string line) =>
        line.StartsWith("Team Duration", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Requested Roles", StringComparison.OrdinalIgnoreCase)
        || line.StartsWith("Duration", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^\d+\.\s*", RegexOptions.None)]
    private static partial Regex NumberedBulletPattern();

    [GeneratedRegex(@"\band a\b|\band an\b|\band one\b|\band\b", RegexOptions.IgnoreCase)]
    private static partial Regex RoleSegmentPattern();
}
