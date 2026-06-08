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
