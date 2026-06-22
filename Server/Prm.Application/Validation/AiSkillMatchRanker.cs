using Prm.Application.DTOs.Manager;

namespace Prm.Application.Validation;

public static class AiSkillMatchRanker
{
    public static IReadOnlyList<SkillMatchResultItem> RankMatches(
        string requirement,
        IReadOnlyList<AiCapacityFilter.CandidateSnapshot> candidates,
        IReadOnlyList<SkillMatchResultItem> llmMatches)
    {
        var expandedKeywords = AiSkillMatcher.ExpandRequirement(requirement);
        var llmReasons = llmMatches.ToDictionary(
            match => match.EmployeeId,
            match => match.Reason,
            comparer: EqualityComparer<int>.Default);

        var ranked = candidates
            .Select(candidate =>
            {
                var user = candidate.ResourceProfile.User;
                var skillNames = candidate.ProfileSkills;
                var matchedSkills = AiSkillMatcher.GetMatchedSkillLabels(skillNames, expandedKeywords);
                var tagMatches = candidate.RecentActivityTags
                    .Where(tag => expandedKeywords.Any(keyword =>
                        tag.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || keyword.Contains(tag, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var score = AiSkillMatcher.ScoreCandidateSkills(
                    skillNames,
                    expandedKeywords,
                    user.Department?.ToString(),
                    user.Designation?.ToString());

                score += tagMatches.Count * 10;
                score += candidate.FreeHoursPerWeek / 10;

                var reason = llmReasons.TryGetValue(candidate.ResourceProfile.Id, out var llmReason)
                    && !string.IsNullOrWhiteSpace(llmReason)
                    ? llmReason.Trim()
                    : BuildReason(matchedSkills, tagMatches, candidate.FreeHoursPerWeek);

                return new SkillMatchResultItem(
                    candidate.ResourceProfile.Id,
                    user.FullName,
                    candidate.UtilisationPercent,
                    candidate.AvailabilityPercent,
                    reason,
                    score,
                    matchedSkills);
            })
            .Where(item => item.MatchScore > 0)
            .OrderByDescending(item => item.MatchScore)
            .ThenByDescending(item => item.AvailabilityPercent)
            .Take(3)
            .ToList();

        if (ranked.Count > 0)
        {
            return ranked;
        }

        return candidates
            .OrderByDescending(candidate => candidate.FreeHoursPerWeek)
            .ThenByDescending(candidate => candidate.AvailabilityPercent)
            .Take(3)
            .Select(candidate => new SkillMatchResultItem(
                candidate.ResourceProfile.Id,
                candidate.ResourceProfile.User.FullName,
                candidate.UtilisationPercent,
                candidate.AvailabilityPercent,
                llmReasons.TryGetValue(candidate.ResourceProfile.Id, out var reason)
                    ? reason
                    : "Ranked by available capacity across the organization.",
                0,
                []))
            .ToList();
    }

    private static string BuildReason(
        IReadOnlyList<string> matchedSkills,
        IReadOnlyList<string> tagMatches,
        int freeHours)
    {
        if (matchedSkills.Count > 0)
        {
            return $"{string.Join(", ", matchedSkills)} align with the requirement; {freeHours} hrs/week available.";
        }

        if (tagMatches.Count > 0)
        {
            return $"Recent work on {string.Join(", ", tagMatches)}; {freeHours} hrs/week available.";
        }

        return $"Strong availability ({freeHours} hrs/week) with relevant profile data.";
    }
}
