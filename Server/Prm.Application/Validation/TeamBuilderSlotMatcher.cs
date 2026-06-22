using Prm.Application.Common;
using Prm.Application.DTOs.Manager;

namespace Prm.Application.Validation;

public static class TeamBuilderSlotMatcher
{
    public const int MinimumMatchScore = 30;

    public static IReadOnlyList<TeamBuilderRoleResultDto> MatchSlots(
        IReadOnlyList<TeamBuilderRoleResultDto> roles,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableCandidates,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> allCandidates)
    {
        _ = allCandidates;

        var assignableByName = assignableCandidates
            .ToDictionary(candidate => candidate.FullName, StringComparer.OrdinalIgnoreCase);

        var assignedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var roleAssignments = new Dictionary<int, TeamBuilderBenchMatchDto>();
        var pendingRoleIndices = Enumerable.Range(0, roles.Count).ToList();

        while (pendingRoleIndices.Count > 0)
        {
            (int RoleIndex, TeamBuilderBenchMatchDto Match, int Margin)? bestPair = null;

            foreach (var roleIndex in pendingRoleIndices)
            {
                var keywords = AiTeamBuilderSkillRanker.ExtractKeywords(roles[roleIndex]);
                var benchMatches = AiTeamBuilderSkillRanker.RankBenchCandidates(
                    keywords,
                    assignableCandidates,
                    assignedNames);

                if (benchMatches.Count == 0 || benchMatches[0].MatchScore < MinimumMatchScore)
                {
                    continue;
                }

                var topScore = benchMatches[0].MatchScore;
                var margin = benchMatches.Count > 1
                    ? topScore - benchMatches[1].MatchScore
                    : topScore;

                if (bestPair is null
                    || topScore > bestPair.Value.Match.MatchScore
                    || (topScore == bestPair.Value.Match.MatchScore && margin > bestPair.Value.Margin))
                {
                    bestPair = (roleIndex, benchMatches[0], margin);
                }
            }

            if (bestPair is null)
            {
                break;
            }

            roleAssignments[bestPair.Value.RoleIndex] = bestPair.Value.Match;
            assignedNames.Add(bestPair.Value.Match.EmployeeName);
            pendingRoleIndices.Remove(bestPair.Value.RoleIndex);
        }

        var matched = new List<TeamBuilderRoleResultDto>();
        for (var index = 0; index < roles.Count; index++)
        {
            var role = roles[index];
            var keywords = AiTeamBuilderSkillRanker.ExtractKeywords(role);
            var benchExclusions = new HashSet<string>(assignedNames, StringComparer.OrdinalIgnoreCase);
            if (roleAssignments.TryGetValue(index, out var filledAssignment))
            {
                benchExclusions.Remove(filledAssignment.EmployeeName);
            }

            var benchMatches = AiTeamBuilderSkillRanker.RankBenchCandidates(
                keywords,
                assignableCandidates,
                benchExclusions);

            if (roleAssignments.TryGetValue(index, out var assignment))
            {
                var matchedSkillText = string.Join(", ", assignment.MatchedSkills);
                matched.Add(role with
                {
                    Status = TeamBuilderConstants.StatusFilled,
                    AssignedEmployeeName = assignment.EmployeeName,
                    MatchScore = assignment.MatchScore,
                    Reason = string.IsNullOrWhiteSpace(matchedSkillText)
                        ? "Fully benched employee available for this role."
                        : $"Best benched match: {matchedSkillText}; 100% available.",
                    Gap = null,
                    BenchMatches = benchMatches
                });
                continue;
            }

            if (TryAcceptFilledRole(role, assignableByName, assignedNames, out var validatedRole))
            {
                matched.Add(validatedRole with { BenchMatches = benchMatches });
                continue;
            }

            matched.Add(role with
            {
                Status = TeamBuilderConstants.StatusGap,
                AssignedEmployeeName = null,
                MatchScore = null,
                Reason = null,
                Gap = BuildGap(role, benchMatches),
                BenchMatches = benchMatches
            });
        }

        return matched;
    }

    private static bool TryAcceptFilledRole(
        TeamBuilderRoleResultDto role,
        IReadOnlyDictionary<string, AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableByName,
        HashSet<string> assignedNames,
        out TeamBuilderRoleResultDto validatedRole)
    {
        validatedRole = role;

        if (role.Status != TeamBuilderConstants.StatusFilled
            || string.IsNullOrWhiteSpace(role.AssignedEmployeeName))
        {
            return false;
        }

        if (!assignableByName.TryGetValue(role.AssignedEmployeeName, out var candidate)
            || !AiTeamBuilderCandidateMapper.IsFullyBenched(candidate)
            || assignedNames.Contains(role.AssignedEmployeeName))
        {
            return false;
        }

        assignedNames.Add(role.AssignedEmployeeName);
        validatedRole = role with { Gap = null };
        return true;
    }

    private static TeamBuilderGapDto BuildGap(
        TeamBuilderRoleResultDto role,
        IReadOnlyList<TeamBuilderBenchMatchDto> benchMatches)
    {
        if (benchMatches.Count > 0 && benchMatches[0].MatchScore > 0)
        {
            return new TeamBuilderGapDto(
                TeamBuilderConstants.GapReasonNoSkill,
                $"No fully benched employee meets the minimum skill match for {role.RoleTitle}. " +
                $"Closest benched candidate: {benchMatches[0].EmployeeName} (score {benchMatches[0].MatchScore}).",
                null,
                null);
        }

        return new TeamBuilderGapDto(
            TeamBuilderConstants.GapReasonNoSkill,
            "No fully benched employee is available for this role.",
            null,
            null);
    }
}
