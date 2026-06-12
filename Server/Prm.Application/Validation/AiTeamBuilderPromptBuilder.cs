using Prm.Application.Common;
using Prm.Domain.Enums;

namespace Prm.Application.Validation;

public static class AiTeamBuilderPromptBuilder
{
    public static string BuildUserPrompt(
        string requirement,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> assignableCandidates,
        IReadOnlyList<AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot> allCandidates)
    {
        var assignableLines = assignableCandidates.Select(FormatCandidate);
        var allLines = allCandidates.Select(FormatCandidateWithAllocations);

        return
            $"Manager requirement:\n{requirement}\n\n" +
            $"Fully benched assignable candidates (utilisation must be 0 — only use these for {TeamBuilderConstants.StatusFilled}):\n" +
            (assignableCandidates.Count == 0
                ? "- (none)"
                : string.Join("\n", assignableLines)) +
            "\n\n" +
            "Full org candidate pool (use for gap explanations when skills exist but employee is allocated):\n" +
            string.Join("\n", allLines);
    }

    public static string BuildSystemPrompt()
    {
        var proficiencyLevels = string.Join(", ",
            Enum.GetNames<ProficiencyLevel>().Select(name => name.ToUpperInvariant()));

        var jsonShape =
            "{\"roles\":[{\"roleTitle\":\"...\",\"requiredSkills\":[{\"skillName\":\"...\",\"minProficiency\":\"...\"}]," +
            $"\"status\":\"{TeamBuilderConstants.StatusFilled}|{TeamBuilderConstants.StatusGap}\"," +
            "\"assignedEmployeeName\":\"...\",\"matchScore\":90,\"reason\":\"...\"," +
            $"\"gap\":{{\"reasonType\":\"{TeamBuilderConstants.GapReasonNoSkill}|{TeamBuilderConstants.GapReasonAllocatedElsewhere}\"," +
            "\"message\":\"...\",\"alternativeEmployeeName\":\"...\",\"availableFromDate\":\"yyyy-MM-dd\"}}]}";

        return $"""
            You are a {TeamBuilderConstants.TeamBuilderSystemPromptKeyword} for an IT services company.

            Given a manager's natural-language team requirement and employee candidate data, return ONLY valid JSON:
            {jsonShape}

            Phase A — Parse the requirement:
            1. Extract each distinct role from the natural-language text.
            2. For each role infer required skills ({proficiencyLevels} when explicitly stated in the text).
            3. Use minProficiency "{TeamBuilderConstants.ProficiencyAny}" when the user did not specify a level.

            Phase B — Match in one pass:
            1. Fill roles ONLY from fully benched assignable candidates (0% utilisation).
            2. Never assign the same employee to two roles.
            3. Match skills case-insensitively; proficiency order: BEGINNER < INTERMEDIATE < ADVANCED.
            4. For unfilled roles set status "{TeamBuilderConstants.StatusGap}" with exactly one of:
               - {TeamBuilderConstants.GapReasonNoSkill}: no employee has required skills at minimum proficiency.
               - {TeamBuilderConstants.GapReasonAllocatedElsewhere}: skilled employee exists but is not fully benched; name closest match and availableFromDate (latest active allocation end date).
            5. Echo requiredSkills on every role result.
            6. FILLED roles must include assignedEmployeeName, matchScore, and reason. GAP roles must include gap and must NOT include assignedEmployeeName.
            7. Return strict JSON only — no markdown or extra text.
            """;
    }

    private static string FormatCandidate(AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot candidate) =>
        $"- ResourceProfileId {candidate.EmployeeId}, UserId {candidate.UserId}: {candidate.FullName}; designation: {candidate.Designation ?? "N/A"}; " +
        $"util {candidate.UtilisationPercent}%; skills: {FormatSkills(candidate.Skills)}";

    private static string FormatCandidateWithAllocations(
        AiTeamBuilderCandidateMapper.TeamBuilderCandidateSnapshot candidate)
    {
        var allocationText = candidate.ActiveAllocations.Count == 0
            ? "none"
            : string.Join("; ", candidate.ActiveAllocations.Select(
                allocation => $"{allocation.ProjectName} until {allocation.ToDate:yyyy-MM-dd}"));

        return $"- ResourceProfileId {candidate.EmployeeId}, UserId {candidate.UserId}: {candidate.FullName}; designation: {candidate.Designation ?? "N/A"}; " +
               $"util {candidate.UtilisationPercent}%; skills: {FormatSkills(candidate.Skills)}; " +
               $"active allocations: {allocationText}";
    }

    private static string FormatSkills(IReadOnlyList<AiTeamBuilderCandidateMapper.SkillSnapshot> skills) =>
        skills.Count == 0
            ? "none"
            : string.Join(", ", skills.Select(skill => $"{skill.Name} ({skill.Proficiency})"));
}
