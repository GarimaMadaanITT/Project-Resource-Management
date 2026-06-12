namespace Prm.Application.DTOs.Manager;

public record SkillMatchRequest(int ProjectId, string Requirement);

public record SkillMatchResultItem(
    int EmployeeId,
    string EmployeeName,
    int UtilisationPercent,
    int AvailabilityPercent,
    string Reason);

public record SkillMatchResponse(
    string ProjectName,
    string Requirement,
    int? ParsedHoursPerWeek,
    int CandidatesConsidered,
    IReadOnlyList<SkillMatchResultItem> Matches,
    string Disclaimer,
    bool UsedFallbackProvider);

public record RiskSummaryResponse(
    int ProjectId,
    string ProjectName,
    string HealthStatus,
    string Summary,
    string Disclaimer,
    bool UsedFallbackProvider);

public record TeamBuilderRequest(string Requirement);

public record TeamBuilderSkillRequirementDto(string SkillName, string MinProficiency);

public record TeamBuilderGapDto(
    string ReasonType,
    string Message,
    string? AlternativeEmployeeName,
    string? AvailableFromDate);

public record TeamBuilderBenchMatchDto(
    int EmployeeId,
    int UserId,
    string EmployeeName,
    string? Designation,
    int MatchScore,
    IReadOnlyList<string> MatchedSkills);

public record TeamBuilderRoleResultDto(
    string RoleTitle,
    IReadOnlyList<TeamBuilderSkillRequirementDto> RequiredSkills,
    string Status,
    string? AssignedEmployeeName,
    int? MatchScore,
    string? Reason,
    TeamBuilderGapDto? Gap,
    IReadOnlyList<TeamBuilderBenchMatchDto> BenchMatches);

public record TeamBuilderResponse(
    string Requirement,
    int CandidatesConsidered,
    int AssignableCandidates,
    IReadOnlyList<TeamBuilderRoleResultDto> Roles,
    string Disclaimer,
    bool UsedFallbackProvider);
