using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Prm.Application.Common;
using Prm.Application.Interfaces;

namespace Prm.Infrastructure.Ai;

public partial class DeterministicLlmProvider
{
    private readonly ILogger<DeterministicLlmProvider> _logger;

    public DeterministicLlmProvider(ILogger<DeterministicLlmProvider> logger)
    {
        _logger = logger;
    }

    public Task<LlmCompletionResult> CompleteFallbackAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Using deterministic LLM fallback (no external API call).");

        var text = systemPrompt.Contains(TeamBuilderConstants.TeamBuilderSystemPromptKeyword, StringComparison.OrdinalIgnoreCase)
            ? BuildTeamBuilderJson(userPrompt)
            : systemPrompt.Contains("resource planning assistant", StringComparison.OrdinalIgnoreCase)
                ? BuildSkillMatchJson(userPrompt)
                : BuildRiskSummaryParagraph(userPrompt);

        return Task.FromResult(new LlmCompletionResult(text, UsedFallbackProvider: true));
    }

    private static string BuildSkillMatchJson(string userPrompt)
    {
        var requirement = ExtractLineValue(userPrompt, "Requirement:");
        var keywords = Tokenize(requirement);
        var candidates = ParseCandidates(userPrompt);

        var ranked = candidates
            .Select(candidate =>
            {
                var skillScore = candidate.Skills.Count(skill =>
                    keywords.Any(keyword => skill.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                var tagScore = candidate.Tags.Count(tag =>
                    keywords.Any(keyword => tag.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                var score = skillScore * 2 + tagScore + candidate.FreeHours / 10m;
                var matchedSkills = candidate.Skills
                    .Where(skill => keywords.Any(keyword => skill.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .Take(2)
                    .ToList();

                var reason = matchedSkills.Count > 0
                    ? $"{string.Join(" and ", matchedSkills)} align with the requirement; {candidate.FreeHours} hrs/week available."
                    : $"Strong bench availability ({candidate.FreeHours} hrs/week) with relevant delivery experience.";

                return new { candidate.Id, candidate.Name, score, reason };
            })
            .OrderByDescending(item => item.score)
            .Take(3)
            .Select(item => new { employeeId = item.Id, reason = item.reason })
            .ToList();

        return JsonSerializer.Serialize(new { matches = ranked });
    }

    private static string BuildTeamBuilderJson(string userPrompt)
    {
        var requirement = ExtractLineValue(userPrompt, "Manager requirement:");
        var assignable = ParseTeamBuilderCandidates(userPrompt, "Fully benched assignable candidates");
        var allCandidates = ParseTeamBuilderCandidates(userPrompt, "Full org candidate pool");
        var roleSegments = SplitRoleSegments(requirement);
        var assignedIds = new HashSet<int>();
        var roles = new List<object>();

        foreach (var segment in roleSegments)
        {
            var roleTitle = ExtractRoleTitle(segment);
            var keywords = Tokenize(segment);
            var requiredSkills = keywords
                .Take(3)
                .Select(keyword => new { skillName = keyword, minProficiency = "INTERMEDIATE" })
                .ToList();

            if (requiredSkills.Count == 0)
            {
                requiredSkills.Add(new { skillName = roleTitle, minProficiency = "INTERMEDIATE" });
            }

            var match = assignable
                .Where(candidate => !assignedIds.Contains(candidate.Id))
                .Select(candidate => new
                {
                    candidate,
                    score = candidate.Skills.Count(skill =>
                        keywords.Any(keyword => skill.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                })
                .Where(item => item.score > 0)
                .OrderByDescending(item => item.score)
                .FirstOrDefault();

            if (match is not null)
            {
                assignedIds.Add(match.candidate.Id);
                roles.Add(new
                {
                    roleTitle,
                    requiredSkills,
                    status = TeamBuilderConstants.StatusFilled,
                    assignedEmployeeName = match.candidate.Name,
                    matchScore = Math.Min(100, match.score * 30 + 40),
                    reason = $"Matched {string.Join(", ", match.candidate.Skills.Take(2))}; fully benched.",
                    gap = (object?)null
                });
                continue;
            }

            var alternative = allCandidates
                .Select(candidate => new
                {
                    candidate,
                    score = candidate.Skills.Count(skill =>
                        keywords.Any(keyword => skill.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                })
                .Where(item => item.score > 0)
                .OrderByDescending(item => item.score)
                .FirstOrDefault();

            if (alternative is not null && alternative.candidate.Utilisation > 0)
            {
                roles.Add(new
                {
                    roleTitle,
                    requiredSkills,
                    status = TeamBuilderConstants.StatusGap,
                    assignedEmployeeName = (string?)null,
                    matchScore = (int?)null,
                    reason = (string?)null,
                    gap = new
                    {
                        reasonType = TeamBuilderConstants.GapReasonAllocatedElsewhere,
                        message = $"{alternative.candidate.Name} has relevant skills but is allocated elsewhere.",
                        alternativeEmployeeName = alternative.candidate.Name,
                        availableFromDate = alternative.candidate.LatestAllocationEnd?.ToString("yyyy-MM-dd")
                    }
                });
            }
            else
            {
                roles.Add(new
                {
                    roleTitle,
                    requiredSkills,
                    status = TeamBuilderConstants.StatusGap,
                    assignedEmployeeName = (string?)null,
                    matchScore = (int?)null,
                    reason = (string?)null,
                    gap = new
                    {
                        reasonType = TeamBuilderConstants.GapReasonNoSkill,
                        message = "No fully benched employee matches the required skills. Consider hiring or training.",
                        alternativeEmployeeName = (string?)null,
                        availableFromDate = (string?)null
                    }
                });
            }
        }

        if (roles.Count == 0)
        {
            roles.Add(new
            {
                roleTitle = "Team Member",
                requiredSkills = new[] { new { skillName = "General", minProficiency = "INTERMEDIATE" } },
                status = TeamBuilderConstants.StatusGap,
                assignedEmployeeName = (string?)null,
                matchScore = (int?)null,
                reason = (string?)null,
                gap = new
                {
                    reasonType = TeamBuilderConstants.GapReasonNoSkill,
                    message = "Could not parse roles from the requirement.",
                    alternativeEmployeeName = (string?)null,
                    availableFromDate = (string?)null
                }
            });
        }

        return JsonSerializer.Serialize(new { roles });
    }

    private static IReadOnlyList<string> SplitRoleSegments(string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement))
        {
            return [];
        }

        return RoleSegmentPattern()
            .Split(requirement)
            .Select(segment => segment.Trim().TrimEnd('.'))
            .Where(segment => segment.Length > 5)
            .ToList();
    }

    private static string ExtractRoleTitle(string segment)
    {
        var needIndex = segment.IndexOf("need ", StringComparison.OrdinalIgnoreCase);
        var withIndex = segment.IndexOf(" with ", StringComparison.OrdinalIgnoreCase);
        var start = needIndex >= 0 ? needIndex + 5 : 0;
        var end = withIndex > start ? withIndex : Math.Min(segment.Length, start + 40);
        var title = segment[start..end].Trim();
        return string.IsNullOrWhiteSpace(title) ? "Role" : title;
    }

    private static IReadOnlyList<TeamBuilderCandidate> ParseTeamBuilderCandidates(string userPrompt, string sectionLabel)
    {
        var sectionStart = userPrompt.IndexOf(sectionLabel, StringComparison.OrdinalIgnoreCase);
        if (sectionStart < 0)
        {
            return [];
        }

        var sectionText = userPrompt[sectionStart..];
        var nextSection = sectionText.IndexOf("\n\n", StringComparison.Ordinal);
        if (nextSection > 0)
        {
            sectionText = sectionText[..nextSection];
        }

        var candidates = new List<TeamBuilderCandidate>();
        foreach (var line in sectionText.Split('\n'))
        {
            if (!line.StartsWith("- ID ", StringComparison.Ordinal))
            {
                continue;
            }

            var idMatch = IdPattern().Match(line);
            if (!idMatch.Success)
            {
                continue;
            }

            var id = int.Parse(idMatch.Groups["id"].Value);
            var name = idMatch.Groups["name"].Value.Trim();
            var utilMatch = UtilPattern().Match(line);
            var utilisation = utilMatch.Success ? int.Parse(utilMatch.Groups["util"].Value) : 0;
            var skills = ExtractTeamBuilderSkills(line);
            DateOnly? latestEnd = null;
            var untilMatches = UntilDatePattern().Matches(line);
            foreach (Match untilMatch in untilMatches)
            {
                if (DateOnly.TryParse(untilMatch.Groups["date"].Value, out var endDate))
                {
                    latestEnd = latestEnd is null || endDate > latestEnd ? endDate : latestEnd;
                }
            }

            candidates.Add(new TeamBuilderCandidate(id, name, utilisation, skills, latestEnd));
        }

        return candidates;
    }

    private static IReadOnlyList<string> ExtractTeamBuilderSkills(string line)
    {
        var index = line.IndexOf("skills:", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return [];
        }

        var segment = line[(index + "skills:".Length)..];
        var allocIndex = segment.IndexOf("active allocations:", StringComparison.OrdinalIgnoreCase);
        if (allocIndex >= 0)
        {
            segment = segment[..allocIndex];
        }

        return segment
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(skill => skill.Split('(')[0].Trim())
            .Where(skill => skill.Length > 0 && !skill.Equals("none", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string BuildRiskSummaryParagraph(string userPrompt)
    {
        var project = ExtractLineValue(userPrompt, "Project:");
        var health = ExtractLineValue(userPrompt, "Health status:");
        var risks = userPrompt
            .Split("Risk flags:", StringSplitOptions.RemoveEmptyEntries)[^1]
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith('-'))
            .Select(line => line.TrimStart('-', ' '))
            .Where(line => !line.Contains("No critical risk flags", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (risks.Count == 0)
        {
            return $"{project} appears {health.ToLowerInvariant()} based on current milestone and timesheet data. " +
                   "Continue monitoring delivery pace and confirm upcoming milestones remain on schedule.";
        }

        var joined = string.Join(" ", risks.Take(3));
        return $"{project} requires attention ({health}). {joined} " +
               "The manager should follow up with the team, validate blockers, and adjust timeline or staffing if needed.";
    }

    private static string ExtractLineValue(string text, string label)
    {
        var lines = text.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            if (!lines[index].StartsWith(label, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var sameLine = lines[index][label.Length..].Trim();
            if (!string.IsNullOrWhiteSpace(sameLine))
            {
                return sameLine;
            }

            for (var nextIndex = index + 1; nextIndex < lines.Length; nextIndex++)
            {
                var nextLine = lines[nextIndex].Trim();
                if (nextLine.Length == 0)
                {
                    continue;
                }

                if (nextLine.StartsWith('-')
                    || nextLine.Contains("candidates", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                return nextLine;
            }
        }

        return string.Empty;
    }

    private static IReadOnlyList<ParsedCandidate> ParseCandidates(string userPrompt)
    {
        var candidates = new List<ParsedCandidate>();
        foreach (var line in userPrompt.Split('\n'))
        {
            if (!line.StartsWith("- ID ", StringComparison.Ordinal))
            {
                continue;
            }

            var idMatch = IdPattern().Match(line);
            if (!idMatch.Success)
            {
                continue;
            }

            var id = int.Parse(idMatch.Groups["id"].Value);
            var name = idMatch.Groups["name"].Value.Trim();
            var freeMatch = FreeHoursPattern().Match(line);
            var freeHours = freeMatch.Success ? int.Parse(freeMatch.Groups["hours"].Value) : 0;
            var skills = ExtractListSegment(line, "skills:");
            var tags = ExtractListSegment(line, "recent tags:");

            candidates.Add(new ParsedCandidate(id, name, freeHours, skills, tags));
        }

        return candidates;
    }

    private static IReadOnlyList<string> ExtractListSegment(string line, string label)
    {
        var index = line.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return [];
        }

        var segment = line[(index + label.Length)..];
        var tagsIndex = segment.IndexOf("recent tags:", StringComparison.OrdinalIgnoreCase);
        if (label.Equals("skills:", StringComparison.OrdinalIgnoreCase) && tagsIndex >= 0)
        {
            segment = segment[..tagsIndex];
        }

        return segment
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return WordPattern()
            .Matches(text.ToLowerInvariant())
            .Select(match => match.Value)
            .Where(word => word.Length > 2)
            .Distinct()
            .ToList();
    }

    [GeneratedRegex(@"- ID (?<id>\d+): (?<name>[^;]+);")]
    private static partial Regex IdPattern();

    [GeneratedRegex(@"free (?<hours>\d+) hrs/week")]
    private static partial Regex FreeHoursPattern();

    [GeneratedRegex(@"util (?<util>\d+)%")]
    private static partial Regex UtilPattern();

    [GeneratedRegex(@"until (?<date>\d{4}-\d{2}-\d{2})")]
    private static partial Regex UntilDatePattern();

    [GeneratedRegex(@"\band a\b|\band an\b", RegexOptions.IgnoreCase)]
    private static partial Regex RoleSegmentPattern();

    [GeneratedRegex(@"\b[a-z]{3,}\b")]
    private static partial Regex WordPattern();

    private sealed record ParsedCandidate(
        int Id,
        string Name,
        int FreeHours,
        IReadOnlyList<string> Skills,
        IReadOnlyList<string> Tags);

    private sealed record TeamBuilderCandidate(
        int Id,
        string Name,
        int Utilisation,
        IReadOnlyList<string> Skills,
        DateOnly? LatestAllocationEnd);
}
