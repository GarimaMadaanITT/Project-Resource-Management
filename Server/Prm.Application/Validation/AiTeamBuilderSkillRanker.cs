using Prm.Application.Common;
using Prm.Application.DTOs.Manager;
using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class AiTeamBuilderSkillRanker
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "need", "want", "find", "hire", "with", "and", "for", "the", "new", "team", "member", "role"
    };

    public static IReadOnlyList<TeamBuilderRoleResultDto> EnrichAndCorrectRoles(
        IReadOnlyList<TeamBuilderRoleResultDto> roles,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableCandidates,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> allCandidates)
    {
        var assignedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enriched = new List<TeamBuilderRoleResultDto>();

        foreach (var role in roles)
        {
            var keywords = ExtractKeywords(role);
            var benchMatches = RankBenchCandidates(keywords, assignableCandidates, assignedNames);
            var corrected = CorrectRole(role, benchMatches, allCandidates, keywords, assignedNames);
            enriched.Add(corrected with { BenchMatches = benchMatches });
        }

        return enriched;
    }

    public static IReadOnlyList<string> ExtractKeywords(TeamBuilderRoleResultDto role)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in role.RequiredSkills)
        {
            if (!string.IsNullOrWhiteSpace(skill.SkillName)
                && !skill.SkillName.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                keywords.Add(skill.SkillName.Trim());
            }
        }

        foreach (var word in Tokenize(role.RoleTitle))
        {
            keywords.Add(word);
        }

        return keywords.ToList();
    }

    public static IReadOnlyList<string> ExtractKeywordsFromText(string text)
    {
        return Tokenize(text).ToList();
    }

    public static IReadOnlyList<TeamBuilderBenchMatchDto> RankBenchCandidates(
        IReadOnlyList<string> keywords,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableCandidates,
        IReadOnlySet<string> excludedNames)
    {
        if (keywords.Count == 0)
        {
            return [];
        }

        return assignableCandidates
            .Where(candidate => !excludedNames.Contains(candidate.FullName))
            .Select(candidate => ScoreCandidate(candidate, keywords))
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .Select(item => new TeamBuilderBenchMatchDto(
                item.Candidate.EmployeeId,
                item.Candidate.UserId,
                item.Candidate.FullName,
                item.Candidate.Designation,
                item.Score,
                item.MatchedSkills))
            .ToList();
    }

    private static TeamBuilderRoleResultDto CorrectRole(
        TeamBuilderRoleResultDto role,
        IReadOnlyList<TeamBuilderBenchMatchDto> benchMatches,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> allCandidates,
        IReadOnlyList<string> keywords,
        HashSet<string> assignedNames)
    {
        if (role.Status == TeamBuilderConstants.StatusFilled
            && !string.IsNullOrWhiteSpace(role.AssignedEmployeeName))
        {
            assignedNames.Add(role.AssignedEmployeeName);
            return role;
        }

        if (benchMatches.Count > 0)
        {
            var best = benchMatches[0];
            assignedNames.Add(best.EmployeeName);
            var matchedSkillText = string.Join(", ", best.MatchedSkills);
            return role with
            {
                Status = TeamBuilderConstants.StatusFilled,
                AssignedEmployeeName = best.EmployeeName,
                MatchScore = best.MatchScore,
                Reason = $"Best benched match: {matchedSkillText}; 100% available.",
                Gap = null
            };
        }

        var allocatedAlternative = FindBestAllocatedAlternative(keywords, allCandidates);
        if (allocatedAlternative is not null)
        {
            return role with
            {
                Status = TeamBuilderConstants.StatusGap,
                AssignedEmployeeName = null,
                MatchScore = null,
                Reason = null,
                Gap = new TeamBuilderGapDto(
                    TeamBuilderConstants.GapReasonAllocatedElsewhere,
                    $"{allocatedAlternative.FullName} has relevant skills ({string.Join(", ", allocatedAlternative.MatchedSkills)}) but is not fully benched.",
                    allocatedAlternative.FullName,
                    allocatedAlternative.AvailableFromDate)
            };
        }

        return role with
        {
            Status = TeamBuilderConstants.StatusGap,
            AssignedEmployeeName = null,
            MatchScore = null,
            Reason = null,
            Gap = role.Gap ?? new TeamBuilderGapDto(
                TeamBuilderConstants.GapReasonNoSkill,
                "No fully benched employee matches the required skills. Consider hiring or training.",
                null,
                null)
        };
    }

    private static AllocatedAlternative? FindBestAllocatedAlternative(
        IReadOnlyList<string> keywords,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> allCandidates)
    {
        var best = allCandidates
            .Where(candidate => candidate.UtilisationPercent > 0)
            .Select(candidate => ScoreCandidate(candidate, keywords))
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .FirstOrDefault();

        if (best is null)
        {
            return null;
        }

        var latestEnd = best.Candidate.ActiveAllocations
            .Select(allocation => allocation.ToDate)
            .DefaultIfEmpty()
            .Max();

        return new AllocatedAlternative(
            best.Candidate.FullName,
            best.MatchedSkills,
            latestEnd == default ? null : latestEnd.ToString("yyyy-MM-dd"));
    }

    private static ScoredCandidate ScoreCandidate(
        AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot candidate,
        IReadOnlyList<string> keywords)
    {
        var matchedSkills = candidate.Skills
            .Where(skill => keywords.Any(keyword =>
                skill.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || keyword.Contains(skill.Name, StringComparison.OrdinalIgnoreCase)))
            .Select(skill => $"{skill.Name} ({skill.Proficiency})")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var designationBonus = keywords.Any(keyword =>
            candidate.Designation?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true
            || keyword.Contains("developer", StringComparison.OrdinalIgnoreCase)
                && candidate.Designation?.Contains("Engineer", StringComparison.OrdinalIgnoreCase) == true)
            ? 15
            : 0;

        var score = matchedSkills.Count * 30 + designationBonus;
        return new ScoredCandidate(candidate, score, matchedSkills);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split([' ', ',', '.', ';', ':', '-'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(word => word.Trim())
            .Where(word => word.Length > 2 && !StopWords.Contains(word))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<TeamBuilderSkillRequirementDto> InferRequiredSkills(string segment)
    {
        var proficiency = InferProficiency(segment);
        var keywords = ExtractKeywordsFromText(segment)
            .Where(word => !word.Equals("developer", StringComparison.OrdinalIgnoreCase)
                && !word.Equals("engineer", StringComparison.OrdinalIgnoreCase)
                && !word.Equals("tester", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .Select(word => new TeamBuilderSkillRequirementDto(word, proficiency))
            .ToList();

        if (keywords.Count == 0)
        {
            var title = ExtractRoleTitleFromSegment(segment);
            keywords.Add(new TeamBuilderSkillRequirementDto(title, proficiency));
        }

        return keywords;
    }

    public static string InferProficiency(string text)
    {
        if (text.Contains("beginner", StringComparison.OrdinalIgnoreCase)
            || text.Contains("basic", StringComparison.OrdinalIgnoreCase))
        {
            return ProficiencyLevel.Beginner.ToString().ToUpperInvariant();
        }

        if (text.Contains("advanced", StringComparison.OrdinalIgnoreCase)
            || text.Contains("senior", StringComparison.OrdinalIgnoreCase)
            || text.Contains("expert", StringComparison.OrdinalIgnoreCase))
        {
            return ProficiencyLevel.Advanced.ToString().ToUpperInvariant();
        }

        if (text.Contains("intermediate", StringComparison.OrdinalIgnoreCase))
        {
            return ProficiencyLevel.Intermediate.ToString().ToUpperInvariant();
        }

        return TeamBuilderConstants.ProficiencyAny;
    }

    public static string ExtractRoleTitleFromSegment(string segment)
    {
        var needIndex = segment.IndexOf("need ", StringComparison.OrdinalIgnoreCase);
        var withIndex = segment.IndexOf(" with ", StringComparison.OrdinalIgnoreCase);
        var start = needIndex >= 0 ? needIndex + 5 : 0;
        var end = withIndex > start ? withIndex : segment.Length;
        var title = segment[start..end].Trim();
        if (title.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
        {
            title = title[3..].Trim();
        }
        else if (title.StartsWith("a ", StringComparison.OrdinalIgnoreCase))
        {
            title = title[2..].Trim();
        }

        return string.IsNullOrWhiteSpace(title) ? "Role" : title;
    }

    private sealed record ScoredCandidate(
        AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot Candidate,
        int Score,
        IReadOnlyList<string> MatchedSkills);

    private sealed record AllocatedAlternative(
        string FullName,
        IReadOnlyList<string> MatchedSkills,
        string? AvailableFromDate);
}
