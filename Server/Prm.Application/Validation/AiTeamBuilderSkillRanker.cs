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
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> allCandidates) =>
        TeamBuilderSlotMatcher.MatchSlots(roles, assignableCandidates, allCandidates);

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

    internal static ScoredCandidate ScoreCandidate(
        AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot candidate,
        IReadOnlyList<string> keywords)
    {
        var expandedKeywords = AiSkillMatcher.ExpandKeywords(keywords);
        var skillNames = candidate.Skills.Select(skill => skill.Name).ToList();
        var matchedSkillNames = AiSkillMatcher.GetMatchedSkillLabels(skillNames, expandedKeywords);

        var matchedSkills = candidate.Skills
            .Where(skill => matchedSkillNames.Contains(skill.Name, StringComparer.OrdinalIgnoreCase))
            .Select(skill => $"{skill.Name} ({skill.Proficiency})")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var score = AiSkillMatcher.ScoreCandidateSkills(
            skillNames,
            expandedKeywords,
            candidate.Department,
            candidate.Designation);

        if (matchedSkills.Count > 0 && score == 0)
        {
            score = matchedSkills.Count * 30;
        }

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

    internal sealed record ScoredCandidate(
        AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot Candidate,
        int Score,
        IReadOnlyList<string> MatchedSkills);
}
