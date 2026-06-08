namespace Prm.Application.DTOs.Admin;

public record EmployeeListItemDto(
    int Id,
    string Name,
    string Department,
    string Status,
    bool IsActive,
    int UtilisationPercent);

public record EmployeeListResponse(
    IReadOnlyList<EmployeeListItemDto> Employees,
    int Total,
    int AllocatedCount,
    int BenchCount);

public record UpdateEmployeeRequest(string Department);

public record EmployeeSkillDto(int SkillId, string Name, string Category, string Proficiency);

public record AddEmployeeSkillRequest(string SkillName, string Category, string Proficiency);

public record UpdateEmployeeSkillRequest(string Proficiency);

public record AssignManagerRequest(int ManagerUserId);

public record DeactivateEmployeeResponse(string Message, int EndedAllocations);

public record CreateUserRequest(
    string FullName,
    string Email,
    string Username,
    string TemporaryPassword,
    string Role,
    string? Department);

public record UserListItemDto(
    int Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive);

public record ResetPasswordRequest(string NewTemporaryPassword);

public record ProjectListItemDto(
    int Id,
    string Name,
    string ManagerName,
    DateOnly EndDate,
    string Status,
    int StoryPointsDone,
    int TotalStoryPoints);

public record CreateProjectRequest(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int ManagerUserId,
    int TotalStoryPoints);

public record UpdateProjectRequest(
    string Name,
    string Description,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    int ManagerUserId,
    int TotalStoryPoints);

public record MilestoneDto(
    int Id,
    string Title,
    DateOnly DueDate,
    int StoryPoints,
    string Status);

public record AddMilestoneRequest(
    string Title,
    DateOnly DueDate,
    int StoryPoints);

public record UpdateMilestoneStatusRequest(string Status);

public record AllocationListItemDto(
    string EmployeeName,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record SystemSettingsDto(
    string LlmProvider,
    string LlmApiKeyMasked,
    int SchedulerIntervalHours,
    int MaxWeeklyHours);

public record UpdateSystemSettingsRequest(
    string? LlmProvider,
    string? LlmApiKey,
    int? SchedulerIntervalHours,
    int? MaxWeeklyHours);
