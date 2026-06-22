namespace Prm.Client.Models;

public record LoginRequest(string Username, string Password);

public record AuthenticatedUserModel(
    int Id,
    string Username,
    string FullName,
    string Email,
    string Role);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    AuthenticatedUserModel User,
    bool IsTemporaryPassword);

public record ChangePasswordRequest(string NewPassword, string ConfirmPassword);

public record ChangePasswordResponse(
    string Message,
    bool IsTemporaryPassword,
    string? Token = null,
    DateTime? ExpiresAt = null);

public record MessageResponse(string Message);

public record EmployeeListItemModel(
    int Id,
    int UserId,
    string Name,
    string Department,
    string? Designation,
    string Status,
    bool IsActive,
    int UtilisationPercent);

public record EmployeeListResponseModel(
    IReadOnlyList<EmployeeListItemModel> Employees,
    int Total,
    int AllocatedCount,
    int BenchCount);

public record EmployeeSkillModel(int SkillId, string Name, string Category, string Proficiency);

public record DeactivateEmployeeResponseModel(string Message, int EndedAllocations);

public record UserListItemModel(
    int Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive);

public record ProjectListItemModel(
    int Id,
    string Name,
    string ManagerName,
    DateOnly EndDate,
    string Status,
    int StoryPointsDone,
    int TotalStoryPoints);

public record MilestoneModel(
    int Id,
    string Title,
    DateOnly DueDate,
    int StoryPoints,
    string Status);

public record AllocationListItemModel(
    string EmployeeName,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record SystemSettingsModel(
    string LlmProvider,
    string LlmApiKeyMasked,
    int SchedulerIntervalHours,
    int MaxWeeklyHours);

public record ManagerDashboardEmployeeModel(
    int Id,
    string Name,
    string Department,
    int UtilisationPercent,
    int AvailabilityPercent,
    string SkillsSummary);

public record ManagerDashboardResponseModel(
    IReadOnlyList<ManagerDashboardEmployeeModel> Bench,
    IReadOnlyList<ManagerDashboardEmployeeModel> Partial,
    IReadOnlyList<ManagerDashboardEmployeeModel> Full,
    int BenchCount,
    int PartialCount,
    int FullCount);

public record ManagerAllocationItemModel(
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record ManagerEmployeeDetailModel(
    int Id,
    string Name,
    string Department,
    string Status,
    int UtilisationPercent,
    IReadOnlyList<string> ProfileSkills,
    IReadOnlyList<ManagerAllocationItemModel> ActiveAllocations,
    IReadOnlyList<string> RecentActivityTags);

public record ManagerAllocationModel(
    int Id,
    int ProjectId,
    string ProjectName,
    int EmployeeId,
    string EmployeeName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record EndAllocationResponseModel(string Message, string EmployeeStatus);

public record ManagerProjectListItemModel(
    int Id,
    string Name,
    DateOnly EndDate,
    string HealthStatus);

public record ManagerRiskFlagModel(string Code, string Message, bool IsRisk);

public record ManagerMilestoneModel(
    int Id,
    string Title,
    DateOnly DueDate,
    string Status,
    bool IsOverdue);

public record ManagerProjectDetailModel(
    int Id,
    string Name,
    string Description,
    DateOnly EndDate,
    string HealthStatus,
    IReadOnlyList<ManagerRiskFlagModel> RiskFlags,
    IReadOnlyList<ManagerMilestoneModel> Milestones,
    IReadOnlyList<ManagerAllocationItemModel> Allocations);

public record TeamTimesheetRowModel(
    string EmployeeName,
    string ProjectName,
    decimal Hours,
    string Status,
    bool TimesheetSubmissionFrozen);

public record TeamTimesheetsResponseModel(
    DateOnly WeekStart,
    IReadOnlyList<TeamTimesheetRowModel> Rows);

public record ManagerTimesheetEntryDetailModel(
    string ProjectName,
    decimal Hours,
    IReadOnlyList<string> ActivityTags);

public record ManagerEmployeeTimesheetDetailModel(
    int EmployeeId,
    string EmployeeName,
    DateOnly WeekStart,
    string Status,
    bool TimesheetSubmissionFrozen,
    IReadOnlyList<ManagerTimesheetEntryDetailModel> Entries);

public record RestoreTimesheetAccessResponseModel(
    int EmployeeId,
    string EmployeeName,
    bool TimesheetSubmissionFrozen,
    string Message);

public record ActivityTagsResponseModel(
    IReadOnlyList<string> PredefinedTags,
    bool AllowsCustomOther);

public record TimesheetListItemModel(
    DateOnly WeekStart,
    decimal TotalHours,
    string Status);

public record MyTimesheetsResponseModel(IReadOnlyList<TimesheetListItemModel> Weeks);

public record TimesheetEntryDetailModel(
    string ProjectName,
    decimal Hours,
    IReadOnlyList<string> ActivityTags);

public record TimesheetWeekDetailModel(
    DateOnly WeekStart,
    decimal TotalHours,
    string Status,
    IReadOnlyList<TimesheetEntryDetailModel> Entries);

public record SubmitTimesheetResponseModel(
    DateOnly WeekStart,
    decimal TotalHours,
    string Status,
    string Message);

public record EmployeeAllocationItemModel(
    int ProjectId,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate,
    string Status);

public record MyAllocationsResponseModel(
    IReadOnlyList<EmployeeAllocationItemModel> Allocations,
    int TotalUtilisationPercent);

public record EmployeeReminderResponseModel(
    bool HasReminder,
    DateOnly? WeekStart,
    string? Message);

public record SkillMatchResultModel(
    int EmployeeId,
    string EmployeeName,
    int UtilisationPercent,
    int AvailabilityPercent,
    string Reason,
    int MatchScore = 0,
    IReadOnlyList<string>? MatchedSkills = null);

public record SkillMatchResponseModel(
    string ProjectName,
    string Requirement,
    int? ParsedHoursPerWeek,
    int CandidatesConsidered,
    IReadOnlyList<SkillMatchResultModel> Matches,
    string Disclaimer,
    bool UsedFallbackProvider);

public record RiskSummaryResponseModel(
    int ProjectId,
    string ProjectName,
    string HealthStatus,
    string Summary,
    string Disclaimer,
    bool UsedFallbackProvider);

public record TeamBuilderSkillRequirementModel(string SkillName, string MinProficiency);

public record TeamBuilderGapModel(
    string ReasonType,
    string Message,
    string? AlternativeEmployeeName,
    string? AvailableFromDate);

public record TeamBuilderBenchMatchModel(
    int EmployeeId,
    int UserId,
    string EmployeeName,
    string? Designation,
    int MatchScore,
    IReadOnlyList<string> MatchedSkills);

public record TeamBuilderRoleResultModel(
    string RoleTitle,
    IReadOnlyList<TeamBuilderSkillRequirementModel> RequiredSkills,
    string Status,
    string? AssignedEmployeeName,
    int? MatchScore,
    string? Reason,
    TeamBuilderGapModel? Gap,
    IReadOnlyList<TeamBuilderBenchMatchModel> BenchMatches);

public record TeamBuilderResponseModel(
    string Requirement,
    int CandidatesConsidered,
    int AssignableCandidates,
    IReadOnlyList<TeamBuilderRoleResultModel> Roles,
    string Disclaimer,
    bool UsedFallbackProvider);

public record AuditLogItemModel(
    int Id,
    string EntityName,
    int EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    int? PerformedByUserId,
    string? PerformedByRole,
    string Source,
    DateTime CreatedAtUtc);

public record AuditLogListResponseModel(
    IReadOnlyList<AuditLogItemModel> Items,
    int Page,
    int PageSize,
    int TotalCount);
