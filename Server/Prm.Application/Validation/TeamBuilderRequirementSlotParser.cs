using System.Text.RegularExpressions;
using Prm.Application.Common;
using Prm.Application.DTOs.Manager;

namespace Prm.Application.Validation;

public static partial class TeamBuilderRequirementSlotParser
{
    public sealed record RoleSlot(string RoleTitle, string SourceSegment);

    public static IReadOnlyList<RoleSlot> ParseSlots(string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement))
        {
            return [];
        }

        var counted = ParseCountedSlots(requirement);
        if (counted.Count > 0)
        {
            return counted;
        }

        var bulletSegments = ExtractBulletSegments(requirement);
        if (bulletSegments.Count > 0)
        {
            return bulletSegments
                .Select(segment => new RoleSlot(NormalizeRoleTitle(segment), segment))
                .ToList();
        }

        var segments = AiTeamBuilderRequirementAnalyzer.SplitRoleSegments(requirement);
        return segments
            .Select(segment => new RoleSlot(
                AiTeamBuilderSkillRanker.ExtractRoleTitleFromSegment(segment),
                segment))
            .ToList();
    }

    public static IReadOnlyList<TeamBuilderRoleResultDto> ToUnresolvedRoles(IReadOnlyList<RoleSlot> slots) =>
        slots.Select(slot => new TeamBuilderRoleResultDto(
            slot.RoleTitle,
            AiTeamBuilderSkillRanker.InferRequiredSkills(slot.SourceSegment),
            TeamBuilderConstants.StatusGap,
            null,
            null,
            null,
            null,
            [])).ToList();

    public static IReadOnlyList<TeamBuilderRoleResultDto> MergeLlmSkills(
        IReadOnlyList<TeamBuilderRoleResultDto> slots,
        IReadOnlyList<TeamBuilderRoleResultDto> llmRoles)
    {
        if (llmRoles.Count == 0)
        {
            return slots;
        }

        var merged = new List<TeamBuilderRoleResultDto>();
        var usedLlmIndices = new HashSet<int>();

        foreach (var slot in slots)
        {
            var llmIndex = FindBestLlmRoleIndex(slot.RoleTitle, llmRoles, usedLlmIndices);
            if (llmIndex >= 0)
            {
                usedLlmIndices.Add(llmIndex);
                var llmRole = llmRoles[llmIndex];
                merged.Add(slot with
                {
                    RequiredSkills = llmRole.RequiredSkills.Count > 0
                        ? llmRole.RequiredSkills
                        : slot.RequiredSkills
                });
            }
            else
            {
                merged.Add(slot);
            }
        }

        return merged;
    }

    private static IReadOnlyList<RoleSlot> ParseCountedSlots(string requirement)
    {
        var normalized = TrailingDurationPattern().Replace(requirement.Trim(), string.Empty).Trim();
        normalized = ImplicitCountPattern().Replace(normalized, "1 ");
        var matches = CountedRolePattern().Matches(normalized);
        if (matches.Count == 0)
        {
            return [];
        }

        var slots = new List<RoleSlot>();
        foreach (Match match in matches)
        {
            if (!int.TryParse(match.Groups["count"].Value, out var count) || count <= 0)
            {
                continue;
            }

            var segment = match.Groups["role"].Value.Trim().TrimEnd('.', ',');
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            var title = NormalizeRoleTitle(segment);
            for (var i = 0; i < count; i++)
            {
                slots.Add(new RoleSlot(title, segment));
            }
        }

        return slots;
    }

    private static List<string> ExtractBulletSegments(string requirement)
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

            if (line.Length <= 2)
            {
                continue;
            }

            results.Add(line);
        }

        return results;
    }

    private static int FindBestLlmRoleIndex(
        string roleTitle,
        IReadOnlyList<TeamBuilderRoleResultDto> llmRoles,
        IReadOnlySet<int> usedIndices)
    {
        var bestIndex = -1;
        var bestScore = 0;

        for (var index = 0; index < llmRoles.Count; index++)
        {
            if (usedIndices.Contains(index))
            {
                continue;
            }

            var score = ScoreTitleSimilarity(roleTitle, llmRoles[index].RoleTitle);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = index;
            }
        }

        return bestScore > 0 ? bestIndex : -1;
    }

    private static int ScoreTitleSimilarity(string left, string right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return 0;
        }

        if (left.Equals(right, StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (left.Contains(right, StringComparison.OrdinalIgnoreCase)
            || right.Contains(left, StringComparison.OrdinalIgnoreCase))
        {
            return 70;
        }

        var leftTokens = AiSkillMatcher.Tokenize(left).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightTokens = AiSkillMatcher.Tokenize(right).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var overlap = leftTokens.Intersect(rightTokens, StringComparer.OrdinalIgnoreCase).Count();

        return overlap * 20;
    }

    internal static string NormalizeRoleTitle(string segment)
    {
        var cleaned = segment.Trim().TrimEnd('.', ',');
        cleaned = DurationSuffixPattern().Replace(cleaned, string.Empty).Trim();

        foreach (var (pattern, title) in KnownRoleTitles)
        {
            if (pattern.IsMatch(cleaned))
            {
                return title;
            }
        }

        return ToTitleCase(cleaned);
    }

    private static string ToTitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Role";
        }

        return string.Join(' ',
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    private static readonly (Regex Pattern, string Title)[] KnownRoleTitles =
    [
        (new Regex(@"\bdevops\b", RegexOptions.IgnoreCase), "DevOps Engineer"),
        (new Regex(@"\bjava\s+dev(?:elop(?:er)?|olper|elpoer)?\b", RegexOptions.IgnoreCase), "Java Developer"),
        (new Regex(@"\bsedts?\b", RegexOptions.IgnoreCase), "SDET"),
        (new Regex(@"\bsdet\b", RegexOptions.IgnoreCase), "SDET"),
        (new Regex(@"\bqa\b|\bquality assurance\b|\btester\b", RegexOptions.IgnoreCase), "QA Tester"),
        (new Regex(@"\bpython\s+dev(?:elop(?:er)?)?\b", RegexOptions.IgnoreCase), "Python Developer"),
        (new Regex(@"\bfrontend\b|\bfront end\b", RegexOptions.IgnoreCase), "Frontend Developer"),
        (new Regex(@"\bbackend\b|\bback end\b", RegexOptions.IgnoreCase), "Backend Developer"),
    ];

    [GeneratedRegex(@"(?<![a-zA-Z])(?:a|an|one)\s+(?=[a-zA-Z])", RegexOptions.IgnoreCase)]
    private static partial Regex ImplicitCountPattern();

    [GeneratedRegex(@"\b(?<count>\d+)\s+(?<role>[a-zA-Z][a-zA-Z\s]{0,50}?)(?=\s+\d+\s+|\s+and\s+\d+\s+|\s+for\b|\s*$|,)", RegexOptions.IgnoreCase)]
    private static partial Regex CountedRolePattern();

    [GeneratedRegex(@"\s+for\s+\d+\s*(?:months?|weeks?|years?)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingDurationPattern();

    [GeneratedRegex(@"\s+for\s+\d+\s*(?:months?|weeks?|years?)?\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex DurationSuffixPattern();

    [GeneratedRegex(@"^\d+\.\s*", RegexOptions.None)]
    private static partial Regex NumberedBulletPattern();
}
