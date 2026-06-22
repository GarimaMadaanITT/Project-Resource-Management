namespace Prm.Application.DTOs.Manager;

public record ManagerDashboardEmployeeDto(
    int Id,
    string Name,
    string Department,
    int UtilisationPercent,
    int AvailabilityPercent,
    string SkillsSummary);

public record ManagerDashboardResponse(
    IReadOnlyList<ManagerDashboardEmployeeDto> Bench,
    IReadOnlyList<ManagerDashboardEmployeeDto> Partial,
    IReadOnlyList<ManagerDashboardEmployeeDto> Full,
    int BenchCount,
    int PartialCount,
    int FullCount);

public record ManagerAllocationItemDto(
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record ManagerEmployeeDetailDto(
    int Id,
    string Name,
    string Department,
    string Status,
    int UtilisationPercent,
    IReadOnlyList<string> ProfileSkills,
    IReadOnlyList<ManagerAllocationItemDto> ActiveAllocations,
    IReadOnlyList<string> RecentActivityTags);
