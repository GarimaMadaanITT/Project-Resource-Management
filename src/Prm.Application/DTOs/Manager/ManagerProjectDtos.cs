namespace Prm.Application.DTOs.Manager;

public record ManagerProjectListItemDto(
    int Id,
    string Name,
    DateOnly EndDate,
    string HealthStatus);

public record ManagerMilestoneDto(
    int Id,
    string Title,
    DateOnly DueDate,
    string Status,
    bool IsOverdue);

public record ManagerProjectAllocationDto(
    string EmployeeName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record ManagerRiskFlagDto(
    string Code,
    string Message,
    bool IsRisk);

public record ManagerProjectDetailDto(
    int Id,
    string Name,
    string Description,
    DateOnly EndDate,
    string HealthStatus,
    IReadOnlyList<ManagerRiskFlagDto> RiskFlags,
    IReadOnlyList<ManagerMilestoneDto> Milestones,
    IReadOnlyList<ManagerProjectAllocationDto> Allocations);
