namespace Prm.Application.DTOs.Employee;

public record EmployeeAllocationItemDto(
    int ProjectId,
    string ProjectName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate,
    string Status);

public record MyAllocationsResponse(
    IReadOnlyList<EmployeeAllocationItemDto> Allocations,
    int TotalUtilisationPercent);
