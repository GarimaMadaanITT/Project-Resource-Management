namespace Prm.Application.DTOs.Manager;

public record CreateManagerAllocationRequest(
    int ProjectId,
    int EmployeeId,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record ManagerAllocationDto(
    int Id,
    int ProjectId,
    string ProjectName,
    int EmployeeId,
    string EmployeeName,
    int UtilisationPercent,
    DateOnly FromDate,
    DateOnly ToDate);

public record EndAllocationRequest(DateOnly? EndDate);

public record EndAllocationResponse(string Message, string EmployeeStatus);
